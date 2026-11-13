using System.Text;

namespace Ares.MapData.Packages;

/// <summary>Object path spelling: <c>Package.Object:Subobject.Deeper</c>.</summary>
internal static class ObjectPath
{
    /// <summary>
    /// Joins names listed from the innermost object outwards; the last one is the package. A top-level object follows
    /// its package after <c>.</c>, its direct subobjects follow it after <c>:</c> and deeper subobjects after <c>.</c>.
    /// </summary>
    public static string Join(IReadOnlyList<string> innermostFirst)
    {
        StringBuilder path = new();
        for (int depth = 0; depth < innermostFirst.Count; depth++)
        {
            if (depth > 0)
            {
                path.Append(depth == 2 ? ':' : '.');
            }

            path.Append(innermostFirst[innermostFirst.Count - 1 - depth]);
        }

        return path.ToString();
    }
}
