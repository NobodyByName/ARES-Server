using Ares.GameDataExtract.Packages;
using Ares.Structs.Core.Assets;

namespace Ares.GameDataExtract.Properties;

/// <summary>
/// Reads chosen members of a property block up to the last of them, so members after it (often of native or unknown
/// types) need not be understood.
/// </summary>
internal static class SchemaPrefix
{
    /// <summary>The members <paramref name="names"/> the block of <paramref name="export"/> stores.</summary>
    /// <exception cref="InvalidDataException">A member is not in the chain, or the block stores an unknown member before one of them.</exception>
    public static PropertyBag Read(CookedPackage package, ZenExport export, StructSchema schema, IReadOnlyList<string> names)
    {
        int last = names.Max(name => IndexOf(schema, name));
        StructSchema truncated = Truncated(schema, last);
        int start = CookedPackage.StartOf(export);
        UnversionedHeader header = UnversionedHeader.Read(package.Data, start);
        if (header.Members.FirstOrDefault(member => member.IsNonZero && member.Index <= last && truncated.At(member.Index).Field is null) is { IsNonZero: true } unknown)
        {
            throw new InvalidDataException(
                $"{package.Header.ExportPath(export.Index)} stores the unknown member {unknown.Index} of {schema.Name} before {string.Join(", ", names)}");
        }

        return PropertyReader.ReadPrefix(package, start, truncated, out _);
    }

    /// <summary>The chain index of member <paramref name="name"/>.</summary>
    /// <exception cref="InvalidDataException">The chain has no such member.</exception>
    public static int IndexOf(StructSchema schema, string name)
    {
        for (int i = 0; i < schema.ChainLength; i++)
        {
            if (schema.At(i) is { Field.Name: var found, Element: 0 } && found == name)
            {
                return i;
            }
        }

        throw new InvalidDataException($"{schema.Name} has no member {name}");
    }

    /// <summary>The chain of <paramref name="schema"/> up to index <paramref name="last"/>: later members read as unknown.</summary>
    private static StructSchema Truncated(StructSchema schema, int last)
    {
        List<PropertyField?> members = [];
        for (int i = 0; i <= last; i++)
        {
            (PropertyField? field, int element) = schema.At(i);
            if (field is null)
            {
                members.Add(null);
            }
            else if (element == 0)
            {
                members.Add(field);
                i += field.ArrayDim - 1;
            }
        }

        return new StructSchema(schema.Name, members);
    }
}
