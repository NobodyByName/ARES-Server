/* Content labels, implementation states and player-visible support notes. */
globalThis.AresContent = {
  "name": "ARES content",
  "children": [
    {
      "name": "Maps",
      "children": [
        {
          "name": "Dam Battlegrounds",
          "children": [
            {
              "name": "Dam Battlegrounds",
              "technical": "DA_Level_TheDam_02_P",
              "status": "partial",
              "note": "Stock-client entry, lighting, movement, regular doors, ladders and elevator extraction work; combat, loot and settlement remain."
            },
            {
              "name": "Weather & conditions",
              "children": [
                {
                  "name": "Foggy",
                  "technical": "DA_SessionMod_Weather_TheDam_Foggy",
                  "status": "unassessed"
                },
                {
                  "name": "Forest Fire",
                  "technical": "DA_SessionMod_Weather_TheDam_ForestFire",
                  "status": "unassessed"
                },
                {
                  "name": "Frigate Overcast",
                  "technical": "DA_SessionMod_Weather_TheDam_Frigate_Overcast",
                  "status": "unassessed"
                },
                {
                  "name": "Grey Morning",
                  "technical": "DA_SessionMod_Weather_TheDam_GreyMorning",
                  "status": "unassessed"
                },
                {
                  "name": "Hazy Day",
                  "technical": "DA_SessionMod_Weather_TheDam_HazyDay",
                  "status": "unassessed"
                },
                {
                  "name": "Night",
                  "technical": "DA_SessionMod_Weather_TheDam_Night",
                  "status": "unassessed"
                },
                {
                  "name": "Pink Sunrise",
                  "technical": "DA_SessionMod_Weather_TheDam_PinkSunrise",
                  "status": "unassessed"
                },
                {
                  "name": "Rainstorm",
                  "technical": "DA_SessionMod_Weather_TheDam_Rainstorm",
                  "status": "unassessed"
                },
                {
                  "name": "Thunderstorm",
                  "technical": "DA_SessionMod_Weather_TheDam_Thunderstorm",
                  "status": "unassessed"
                },
                {
                  "name": "Windy Storm",
                  "technical": "DA_SessionMod_Weather_TheDam_WindyStorm",
                  "status": "unassessed"
                },
                {
                  "name": "Winter Snow Dam 01",
                  "technical": "DA_SessionMod_Weather_TheDam_WinterSnowDam_01",
                  "status": "unassessed"
                }
              ]
            },
            {
              "name": "Doors & ladders",
              "children": [
                {
                  "name": "Regular doors",
                  "technical": "DamDoors.All",
                  "status": "partial",
                  "note": "120 placed regular doors; door buttons work and barricaded doors can be breached (moving cancels the breach). Unlocking, door blockers and multiplayer interactions need assessment."
                },
                {
                  "name": "Placed ladders",
                  "technical": "DamLadders.All",
                  "status": "partial",
                  "note": "43 placed ladders; climbing is stock-client verified. Multiplayer and traversal validation remain."
                }
              ]
            }
          ]
        },
        {
          "name": "Buried City",
          "children": [
            {
              "name": "Buried City",
              "technical": "DA_Level_BuriedCity_01_P",
              "status": "unassessed",
              "note": "Content candidate; playable availability and server support need review."
            },
            {
              "name": "Weather & conditions",
              "children": [
                {
                  "name": "Buried Day",
                  "technical": "DA_SessionMod_Weather_BuriedCity_BuriedDay",
                  "status": "unassessed"
                },
                {
                  "name": "Frigate Overcast",
                  "technical": "DA_SessionMod_Weather_BuriedCity_Frigate_Overcast",
                  "status": "unassessed"
                },
                {
                  "name": "Golden Overcast",
                  "technical": "DA_SessionMod_Weather_BuriedCity_GoldenOvercast",
                  "status": "unassessed"
                },
                {
                  "name": "Misty Morning",
                  "technical": "DA_SessionMod_Weather_BuriedCity_MistyMorning",
                  "status": "unassessed"
                },
                {
                  "name": "Night",
                  "technical": "DA_SessionMod_Weather_BuriedCity_Night",
                  "status": "unassessed"
                },
                {
                  "name": "Red Dawn",
                  "technical": "DA_SessionMod_Weather_BuriedCity_RedDawn",
                  "status": "unassessed"
                },
                {
                  "name": "Sand Storm",
                  "technical": "DA_SessionMod_Weather_BuriedCity_SandStorm",
                  "status": "unassessed"
                },
                {
                  "name": "Thunderstorm",
                  "technical": "DA_SessionMod_Weather_BuriedCity_Thunderstorm",
                  "status": "unassessed"
                },
                {
                  "name": "Windy Storm",
                  "technical": "DA_SessionMod_Weather_BuriedCity_WindyStorm",
                  "status": "unassessed"
                },
                {
                  "name": "Winter Snow BC 01",
                  "technical": "DA_SessionMod_Weather_BuriedCity_WinterSnowBC_01",
                  "status": "unassessed"
                }
              ]
            },
            {
              "name": "Extraction points",
              "children": [
                {
                  "name": "East",
                  "technical": "DA_ExtractionPoint_BuriedCity_East",
                  "status": "pending",
                  "note": "Extraction definition identified; activation, departure and settlement are not implemented."
                },
                {
                  "name": "North",
                  "technical": "DA_ExtractionPoint_BuriedCity_North",
                  "status": "pending",
                  "note": "Extraction definition identified; activation, departure and settlement are not implemented."
                },
                {
                  "name": "South",
                  "technical": "DA_ExtractionPoint_BuriedCity_South",
                  "status": "pending",
                  "note": "Extraction definition identified; activation, departure and settlement are not implemented."
                },
                {
                  "name": "West",
                  "technical": "DA_ExtractionPoint_BuriedCity_West",
                  "status": "pending",
                  "note": "Extraction definition identified; activation, departure and settlement are not implemented."
                }
              ]
            }
          ]
        },
        {
          "name": "Spaceport",
          "children": [
            {
              "name": "Spaceport",
              "technical": "DA_Level_Spaceport_01_P",
              "status": "unassessed",
              "note": "Content candidate; playable availability and server support need review."
            },
            {
              "name": "Weather & conditions",
              "children": [
                {
                  "name": "Bright Day",
                  "technical": "DA_SessionMod_Weather_Spaceport_BrightDay",
                  "status": "unassessed"
                },
                {
                  "name": "Bright Overcast",
                  "technical": "DA_SessionMod_Weather_Spaceport_BrightOvercast",
                  "status": "unassessed"
                },
                {
                  "name": "Frigate Overcast",
                  "technical": "DA_SessionMod_Weather_Spaceport_Frigate_Overcast",
                  "status": "unassessed"
                },
                {
                  "name": "Night",
                  "technical": "DA_SessionMod_Weather_Spaceport_Night",
                  "status": "unassessed"
                },
                {
                  "name": "Orange Sunset",
                  "technical": "DA_SessionMod_Weather_Spaceport_OrangeSunset",
                  "status": "unassessed"
                },
                {
                  "name": "Pink Morning",
                  "technical": "DA_SessionMod_Weather_Spaceport_PinkMorning",
                  "status": "unassessed"
                },
                {
                  "name": "Thunderstorm",
                  "technical": "DA_SessionMod_Weather_Spaceport_Thunderstorm",
                  "status": "unassessed"
                },
                {
                  "name": "Windy Storm",
                  "technical": "DA_SessionMod_Weather_Spaceport_WindyStorm",
                  "status": "unassessed"
                },
                {
                  "name": "Winter Snow 01",
                  "technical": "DA_SessionMod_Weather_Spaceport_WinterSnow_01",
                  "status": "unassessed"
                }
              ]
            },
            {
              "name": "Extraction points",
              "children": [
                {
                  "name": "Elevator Central",
                  "technical": "DA_ExtractionPoint_Spaceport_Elevator_Central",
                  "status": "pending",
                  "note": "Extraction definition identified; activation, departure and settlement are not implemented."
                },
                {
                  "name": "Elevator East",
                  "technical": "DA_ExtractionPoint_Spaceport_Elevator_East",
                  "status": "pending",
                  "note": "Extraction definition identified; activation, departure and settlement are not implemented."
                },
                {
                  "name": "Elevator South",
                  "technical": "DA_ExtractionPoint_Spaceport_Elevator_South",
                  "status": "pending",
                  "note": "Extraction definition identified; activation, departure and settlement are not implemented."
                },
                {
                  "name": "Elevator West",
                  "technical": "DA_ExtractionPoint_Spaceport_Elevator_West",
                  "status": "pending",
                  "note": "Extraction definition identified; activation, departure and settlement are not implemented."
                }
              ]
            }
          ]
        },
        {
          "name": "Blue Gate",
          "children": [
            {
              "name": "Blue Gate",
              "technical": "DA_Level_TheBlueGate_01_P",
              "status": "unassessed",
              "note": "Content candidate; playable availability and server support need review."
            },
            {
              "name": "Weather & conditions",
              "children": [
                {
                  "name": "Blue Hour",
                  "technical": "DA_SessionMod_Weather_TheBlueGate_BlueHour",
                  "status": "unassessed"
                },
                {
                  "name": "Cold Day",
                  "technical": "DA_SessionMod_Weather_TheBlueGate_ColdDay",
                  "status": "unassessed"
                },
                {
                  "name": "Frigate Overcast",
                  "technical": "DA_SessionMod_Weather_TheBlueGate_Frigate_Overcast",
                  "status": "unassessed"
                },
                {
                  "name": "Frosty Morning",
                  "technical": "DA_SessionMod_Weather_TheBlueGate_FrostyMorning",
                  "status": "unassessed"
                },
                {
                  "name": "Heavy Rain",
                  "technical": "DA_SessionMod_Weather_TheBlueGate_HeavyRain",
                  "status": "unassessed"
                },
                {
                  "name": "Night",
                  "technical": "DA_SessionMod_Weather_TheBlueGate_Night",
                  "status": "unassessed"
                },
                {
                  "name": "Snowy",
                  "technical": "DA_SessionMod_Weather_TheBlueGate_Snowy",
                  "status": "unassessed"
                },
                {
                  "name": "Thunderstorm",
                  "technical": "DA_SessionMod_Weather_TheBlueGate_Thunderstorm",
                  "status": "unassessed"
                },
                {
                  "name": "Windy Storm",
                  "technical": "DA_SessionMod_Weather_TheBlueGate_WindyStorm",
                  "status": "unassessed"
                }
              ]
            }
          ]
        },
        {
          "name": "Riven Tides",
          "children": [
            {
              "name": "Riven Tides",
              "technical": "DA_Level_RivenTides_01_P",
              "status": "unassessed",
              "note": "Content candidate; playable availability and server support need review."
            },
            {
              "name": "Weather & conditions",
              "children": [
                {
                  "name": "Buried Day",
                  "technical": "DA_SessionMod_Weather_RivenTides_BuriedDay",
                  "status": "unassessed"
                },
                {
                  "name": "Riventides Frigate Overcast",
                  "technical": "DA_SessionMod_Weather_Riventides_Frigate_Overcast",
                  "status": "unassessed"
                },
                {
                  "name": "Golden Overcast",
                  "technical": "DA_SessionMod_Weather_RivenTides_GoldenOvercast",
                  "status": "unassessed"
                },
                {
                  "name": "Misty Morning",
                  "technical": "DA_SessionMod_Weather_RivenTides_MistyMorning",
                  "status": "unassessed"
                },
                {
                  "name": "Night",
                  "technical": "DA_SessionMod_Weather_RivenTides_Night",
                  "status": "unassessed"
                },
                {
                  "name": "Red Dawn",
                  "technical": "DA_SessionMod_Weather_RivenTides_RedDawn",
                  "status": "unassessed"
                },
                {
                  "name": "Sand Storm",
                  "technical": "DA_SessionMod_Weather_RivenTides_SandStorm",
                  "status": "unassessed"
                }
              ]
            },
            {
              "name": "Extraction points",
              "children": [
                {
                  "name": "Customs Elevator",
                  "technical": "DA_ExtractionPoint_RivenTides_CustomsElevator",
                  "status": "pending",
                  "note": "Extraction definition identified; activation, departure and settlement are not implemented."
                },
                {
                  "name": "Hilltop Hatch",
                  "technical": "DA_ExtractionPoint_RivenTides_HilltopHatch",
                  "status": "pending",
                  "note": "Extraction definition identified; activation, departure and settlement are not implemented."
                },
                {
                  "name": "Saltswept Hatch",
                  "technical": "DA_ExtractionPoint_RivenTides_SaltsweptHatch",
                  "status": "pending",
                  "note": "Extraction definition identified; activation, departure and settlement are not implemented."
                },
                {
                  "name": "Seaside Elevator",
                  "technical": "DA_ExtractionPoint_RivenTides_SeasideElevator",
                  "status": "pending",
                  "note": "Extraction definition identified; activation, departure and settlement are not implemented."
                },
                {
                  "name": "Weighbridge Hatch",
                  "technical": "DA_ExtractionPoint_RivenTides_WeighbridgeHatch",
                  "status": "pending",
                  "note": "Extraction definition identified; activation, departure and settlement are not implemented."
                }
              ]
            }
          ]
        },
        {
          "name": "Frozen Trail · internal",
          "children": [
            {
              "name": "Frozen Trail · internal",
              "technical": "DA_Level_FrozenTrail_01",
              "status": "unassessed",
              "note": "Content candidate; playable availability and server support need review."
            },
            {
              "name": "Weather & conditions",
              "children": [
                {
                  "name": "Blizzard Day",
                  "technical": "DA_SessionMod_Weather_FrozenTrail_BlizzardDay",
                  "status": "unassessed"
                },
                {
                  "name": "Cloudy Morning",
                  "technical": "DA_SessionMod_Weather_FrozenTrail_CloudyMorning",
                  "status": "unassessed"
                },
                {
                  "name": "Default Day",
                  "technical": "DA_SessionMod_Weather_FrozenTrail_DefaultDay",
                  "status": "unassessed"
                },
                {
                  "name": "Frigate Overcast",
                  "technical": "DA_SessionMod_Weather_FrozenTrail_Frigate_Overcast",
                  "status": "unassessed"
                },
                {
                  "name": "Frozen Day",
                  "technical": "DA_SessionMod_Weather_FrozenTrail_FrozenDay",
                  "status": "unassessed"
                },
                {
                  "name": "Night",
                  "technical": "DA_SessionMod_Weather_FrozenTrail_Night",
                  "status": "unassessed"
                },
                {
                  "name": "Redirection",
                  "technical": "DA_SessionMod_Weather_FrozenTrail_Redirection",
                  "status": "unassessed"
                },
                {
                  "name": "Thunderstorm",
                  "technical": "DA_SessionMod_Weather_FrozenTrail_Thunderstorm",
                  "status": "unassessed"
                }
              ]
            }
          ]
        },
        {
          "name": "Mountain Compound · internal",
          "children": [
            {
              "name": "Mountain Compound · internal",
              "technical": "DA_Level_MountainCompound",
              "status": "unassessed",
              "note": "Content candidate; playable availability and server support need review."
            },
            {
              "name": "Weather & conditions",
              "children": [
                {
                  "name": "Default Day",
                  "technical": "DA_SessionMod_Weather_MountainCompound_DefaultDay",
                  "status": "unassessed"
                },
                {
                  "name": "Emergency Eve",
                  "technical": "DA_SessionMod_Weather_MountainCompound_EmergencyEve",
                  "status": "unassessed"
                },
                {
                  "name": "Night SM",
                  "technical": "DA_SessionMod_Weather_MountainCompound_NightSM",
                  "status": "unassessed"
                },
                {
                  "name": "Snowy Day",
                  "technical": "DA_SessionMod_Weather_MountainCompound_SnowyDay",
                  "status": "unassessed"
                }
              ]
            },
            {
              "name": "Extraction points",
              "children": [
                {
                  "name": "Eastern Tunnel",
                  "technical": "DA_ExtractionPoint_MountainCompound_EasternTunnel",
                  "status": "pending",
                  "note": "Extraction definition identified; activation, departure and settlement are not implemented."
                },
                {
                  "name": "Hatch Assembly",
                  "technical": "DA_ExtractionPoint_MountainCompound_HatchAssembly",
                  "status": "pending",
                  "note": "Extraction definition identified; activation, departure and settlement are not implemented."
                },
                {
                  "name": "Hatch Sandbox",
                  "technical": "DA_ExtractionPoint_MountainCompound_HatchSandbox",
                  "status": "pending",
                  "note": "Extraction definition identified; activation, departure and settlement are not implemented."
                },
                {
                  "name": "Loading Bay",
                  "technical": "DA_ExtractionPoint_MountainCompound_LoadingBay",
                  "status": "pending",
                  "note": "Extraction definition identified; activation, departure and settlement are not implemented."
                },
                {
                  "name": "Lobby Metro Station",
                  "technical": "DA_ExtractionPoint_MountainCompound_LobbyMetroStation",
                  "status": "pending",
                  "note": "Extraction definition identified; activation, departure and settlement are not implemented."
                },
                {
                  "name": "Seed Vault Airshaft",
                  "technical": "DA_ExtractionPoint_MountainCompound_SeedVaultAirshaft",
                  "status": "pending",
                  "note": "Extraction definition identified; activation, departure and settlement are not implemented."
                }
              ]
            }
          ]
        },
        {
          "name": "Onboarding",
          "children": [
            {
              "name": "Onboarding",
              "technical": "DA_Level_OnboardingArray",
              "status": "unassessed",
              "note": "Content candidate; playable availability and server support need review."
            }
          ]
        },
        {
          "name": "Practice Range",
          "children": [
            {
              "name": "Practice Range",
              "technical": "DA_Level_PracticeRangeArray",
              "status": "unassessed",
              "note": "Content candidate; playable availability and server support need review."
            }
          ]
        }
      ]
    },
    {
      "name": "ARC enemies",
      "children": [
        {
          "name": "Bull Crab",
          "technical": "DA_EnemyType_BullCrab",
          "status": "pending",
          "note": "ARC type identified. Server AI, attacks, damage and drops are not implemented. Display uses the internal type name."
        },
        {
          "name": "Chonk",
          "technical": "DA_EnemyType_Chonk",
          "status": "pending",
          "note": "ARC type identified. Server AI, attacks, damage and drops are not implemented. Display uses the internal type name."
        },
        {
          "name": "Chonk Platform Mortar",
          "technical": "DA_EnemyType_ChonkPlatform_Mortar",
          "status": "pending",
          "note": "ARC type identified. Server AI, attacks, damage and drops are not implemented. Display uses the internal type name."
        },
        {
          "name": "Courier",
          "technical": "DA_EnemyType_Courier",
          "status": "pending",
          "note": "ARC type identified. Server AI, attacks, damage and drops are not implemented. Display uses the internal type name."
        },
        {
          "name": "Elite Drone Flamethrower",
          "technical": "DA_EnemyType_EliteDrone_Flamethrower",
          "status": "pending",
          "note": "ARC type identified. Server AI, attacks, damage and drops are not implemented. Display uses the internal type name."
        },
        {
          "name": "Hornet",
          "technical": "DA_EnemyType_LightDroneElite",
          "status": "pending",
          "note": "ARC type identified. Server AI, attacks, damage and drops are not implemented."
        },
        {
          "name": "Roll Bot Flamethrower",
          "technical": "DA_EnemyType_RollBot_Flamethrower",
          "status": "pending",
          "note": "ARC type identified. Server AI, attacks, damage and drops are not implemented. Display uses the internal type name."
        },
        {
          "name": "Heavy Drone Laser",
          "technical": "DA_EnemyType_HeavyDrone_Laser",
          "status": "pending",
          "note": "ARC type identified. Server AI, attacks, damage and drops are not implemented. Display uses the internal type name."
        },
        {
          "name": "Heavy Drone Missile",
          "technical": "DA_EnemyType_HeavyDroneMissile",
          "status": "pending",
          "note": "ARC type identified. Server AI, attacks, damage and drops are not implemented. Display uses the internal type name."
        },
        {
          "name": "Kebab Gatling",
          "technical": "DA_EnemyType_Kebab_Gatling",
          "status": "pending",
          "note": "ARC type identified. Server AI, attacks, damage and drops are not implemented. Display uses the internal type name."
        },
        {
          "name": "Wasp",
          "technical": "DA_EnemyType_LightDrone_02",
          "status": "pending",
          "note": "ARC type identified. Server AI, attacks, damage and drops are not implemented."
        },
        {
          "name": "Meerkat",
          "technical": "DA_EnemyType_Meerkat",
          "status": "pending",
          "note": "ARC type identified. Server AI, attacks, damage and drops are not implemented. Display uses the internal type name."
        },
        {
          "name": "Peppermint",
          "technical": "DA_EnemyType_Peppermint",
          "status": "pending",
          "note": "ARC type identified. Server AI, attacks, damage and drops are not implemented. Display uses the internal type name."
        },
        {
          "name": "Spearmint",
          "technical": "DA_EnemyType_Spearmint",
          "status": "pending",
          "note": "ARC type identified. Server AI, attacks, damage and drops are not implemented. Display uses the internal type name."
        },
        {
          "name": "Monolith",
          "technical": "DA_EnemyType_Monolith",
          "status": "pending",
          "note": "ARC type identified. Server AI, attacks, damage and drops are not implemented. Display uses the internal type name."
        },
        {
          "name": "Pinger",
          "technical": "DA_EnemyType_Pinger",
          "status": "pending",
          "note": "ARC type identified. Server AI, attacks, damage and drops are not implemented. Display uses the internal type name."
        },
        {
          "name": "Roll Bot Pop",
          "technical": "DA_EnemyType_RollBot_Pop",
          "status": "pending",
          "note": "ARC type identified. Server AI, attacks, damage and drops are not implemented. Display uses the internal type name."
        },
        {
          "name": "Rollbot Boom",
          "technical": "DA_EnemyType_Rollbot_Boom",
          "status": "pending",
          "note": "ARC type identified. Server AI, attacks, damage and drops are not implemented. Display uses the internal type name."
        },
        {
          "name": "Roll Bot",
          "technical": "DA_EnemyType_RollBot",
          "status": "pending",
          "note": "ARC type identified. Server AI, attacks, damage and drops are not implemented. Display uses the internal type name."
        },
        {
          "name": "Shredder",
          "technical": "DA_EnemyType_Runner",
          "status": "pending",
          "note": "ARC type identified. Server AI, attacks, damage and drops are not implemented."
        },
        {
          "name": "Sniper",
          "technical": "DA_EnemyType_Sniper",
          "status": "pending",
          "note": "ARC type identified. Server AI, attacks, damage and drops are not implemented. Display uses the internal type name."
        },
        {
          "name": "Snitch Drone",
          "technical": "DA_EnemyType_SnitchDrone",
          "status": "pending",
          "note": "ARC type identified. Server AI, attacks, damage and drops are not implemented. Display uses the internal type name."
        },
        {
          "name": "Snitch Drone Onboarding",
          "technical": "DA_EnemyType_SnitchDrone_Onboarding",
          "status": "pending",
          "note": "ARC type identified. Server AI, attacks, damage and drops are not implemented. Display uses the internal type name."
        },
        {
          "name": "Tarragon 01",
          "technical": "DA_EnemyType_Tarragon_01",
          "status": "pending",
          "note": "ARC type identified. Server AI, attacks, damage and drops are not implemented. Display uses the internal type name."
        },
        {
          "name": "Tick",
          "technical": "DA_EnemyType_Tick",
          "status": "pending",
          "note": "ARC type identified. Server AI, attacks, damage and drops are not implemented. Display uses the internal type name."
        },
        {
          "name": "Rose",
          "technical": "DA_EnemyType_Rose",
          "status": "pending",
          "note": "ARC type identified. Server AI, attacks, damage and drops are not implemented. Display uses the internal type name."
        },
        {
          "name": "Turret Enemy",
          "technical": "DA_EnemyType_TurretEnemy",
          "status": "pending",
          "note": "ARC type identified. Server AI, attacks, damage and drops are not implemented. Display uses the internal type name."
        }
      ]
    },
    {
      "name": "Items & equipment",
      "children": [
        {
          "name": "Gadgets & other items",
          "children": [
            {
              "name": "ARCFriendifier Gadget V2",
              "technical": "DA_Item_ARCFriendifierGadgetV2",
              "status": "unassessed"
            },
            {
              "name": "Adrenalin Shot",
              "technical": "DA_Item_AdrenalinShot",
              "status": "unassessed"
            },
            {
              "name": "Armor Bench 01",
              "technical": "DA_Item_ArmorBench_01",
              "status": "unassessed"
            },
            {
              "name": "Armorpack",
              "technical": "DA_Item_Armorpack",
              "status": "unassessed"
            },
            {
              "name": "Assembly Station 01",
              "technical": "DA_Item_AssemblyStation_01",
              "status": "unassessed"
            },
            {
              "name": "Basic Camera",
              "technical": "DA_Item_BasicCamera",
              "status": "unassessed"
            },
            {
              "name": "Basic Melee Weapon",
              "technical": "DA_Item_BasicMeleeWeapon",
              "status": "unassessed"
            },
            {
              "name": "Basic Sticky Grenade",
              "technical": "DA_Item_BasicStickyGrenade",
              "status": "unassessed"
            },
            {
              "name": "Camera",
              "technical": "DA_Item_Camera",
              "status": "unassessed"
            },
            {
              "name": "Chamber Station Armor Bench 02",
              "technical": "DA_Item_ChamberStation_ArmorBench_02",
              "status": "unassessed"
            },
            {
              "name": "Chamber Station Armor Bench 03",
              "technical": "DA_Item_ChamberStation_ArmorBench_03",
              "status": "unassessed"
            },
            {
              "name": "Chamber Station Assembly Station 02",
              "technical": "DA_Item_ChamberStation_AssemblyStation_02",
              "status": "unassessed"
            },
            {
              "name": "Chamber Station Assembly Station 03",
              "technical": "DA_Item_ChamberStation_AssemblyStation_03",
              "status": "unassessed"
            },
            {
              "name": "Chamber Station Explosive Bench 02",
              "technical": "DA_Item_ChamberStation_ExplosiveBench_02",
              "status": "unassessed"
            },
            {
              "name": "Chamber Station Explosive Bench 03",
              "technical": "DA_Item_ChamberStation_ExplosiveBench_03",
              "status": "unassessed"
            },
            {
              "name": "Chamber Station Potion Station 02",
              "technical": "DA_Item_ChamberStation_PotionStation_02",
              "status": "unassessed"
            },
            {
              "name": "Chamber Station Potion Station 03",
              "technical": "DA_Item_ChamberStation_PotionStation_03",
              "status": "unassessed"
            },
            {
              "name": "Chamber Station Recycle Station 02",
              "technical": "DA_Item_ChamberStation_RecycleStation_02",
              "status": "unassessed"
            },
            {
              "name": "Chamber Station Recycle Station 03",
              "technical": "DA_Item_ChamberStation_RecycleStation_03",
              "status": "unassessed"
            },
            {
              "name": "Chamber Station Research Station 01",
              "technical": "DA_Item_ChamberStation_ResearchStation_01",
              "status": "unassessed"
            },
            {
              "name": "Chamber Station Research Station 02",
              "technical": "DA_Item_ChamberStation_ResearchStation_02",
              "status": "unassessed"
            },
            {
              "name": "Chamber Station Research Station 03",
              "technical": "DA_Item_ChamberStation_ResearchStation_03",
              "status": "unassessed"
            },
            {
              "name": "Chamber Station Research Station 04",
              "technical": "DA_Item_ChamberStation_ResearchStation_04",
              "status": "unassessed"
            },
            {
              "name": "Chamber Station Weapon Bench 04",
              "technical": "DA_Item_ChamberStation_WeaponBench_04",
              "status": "unassessed"
            },
            {
              "name": "Common Binoculars",
              "technical": "DA_Item_CommonBinoculars",
              "status": "unassessed"
            },
            {
              "name": "Consumable Agave",
              "technical": "DA_Item_Consumable_Agave",
              "status": "unassessed"
            },
            {
              "name": "Consumable Agave Juice",
              "technical": "DA_Item_Consumable_AgaveJuice",
              "status": "unassessed"
            },
            {
              "name": "Consumable Apricot",
              "technical": "DA_Item_Consumable_Apricot",
              "status": "unassessed"
            },
            {
              "name": "Consumable Fruit Mix",
              "technical": "DA_Item_Consumable_FruitMix",
              "status": "unassessed"
            },
            {
              "name": "Consumable Lemon",
              "technical": "DA_Item_Consumable_Lemon",
              "status": "unassessed"
            },
            {
              "name": "Consumable Moss",
              "technical": "DA_Item_Consumable_Moss",
              "status": "unassessed"
            },
            {
              "name": "Consumable Mushroom",
              "technical": "DA_Item_Consumable_Mushroom",
              "status": "unassessed"
            },
            {
              "name": "Consumable Olives",
              "technical": "DA_Item_Consumable_Olives",
              "status": "unassessed"
            },
            {
              "name": "Consumable Prickly Pear",
              "technical": "DA_Item_Consumable_PricklyPear",
              "status": "unassessed"
            },
            {
              "name": "Consumable Resin",
              "technical": "DA_Item_Consumable_Resin",
              "status": "unassessed"
            },
            {
              "name": "Consumable Shield Over Time Pack",
              "technical": "DA_Item_Consumable_ShieldOverTimePack",
              "status": "unassessed"
            },
            {
              "name": "Crash Pad",
              "technical": "DA_Item_CrashPad",
              "status": "unassessed"
            },
            {
              "name": "Defibrillator",
              "technical": "DA_Item_Defibrillator",
              "status": "unassessed"
            },
            {
              "name": "Deployable Barricade",
              "technical": "DA_Item_DeployableBarricade",
              "status": "unassessed"
            },
            {
              "name": "Deployable Jump Mine Explosive",
              "technical": "DA_Item_Deployable_JumpMine_Explosive",
              "status": "unassessed"
            },
            {
              "name": "Deployable Remote Flare Trap",
              "technical": "DA_Item_Deployable_RemoteFlareTrap",
              "status": "unassessed"
            },
            {
              "name": "Deployable Sound Trap",
              "technical": "DA_Item_Deployable_SoundTrap",
              "status": "unassessed"
            },
            {
              "name": "Do TGrenade",
              "technical": "DA_Item_DoTGrenade",
              "status": "unassessed"
            },
            {
              "name": "Door Blocker",
              "technical": "DA_Item_DoorBlocker",
              "status": "unassessed"
            },
            {
              "name": "Emitter Trap Tesla Coil",
              "technical": "DA_Item_EmitterTrap_TeslaCoil",
              "status": "unassessed"
            },
            {
              "name": "Explosive Bench 01",
              "technical": "DA_Item_ExplosiveBench_01",
              "status": "unassessed"
            },
            {
              "name": "Firecrackers",
              "technical": "DA_Item_Firecrackers",
              "status": "unassessed"
            },
            {
              "name": "Gadget Instrument Acoustic Guitar",
              "technical": "DA_Item_Gadget_Instrument_AcousticGuitar",
              "status": "unassessed"
            },
            {
              "name": "Gadget Instrument Banjo",
              "technical": "DA_Item_Gadget_Instrument_Banjo",
              "status": "unassessed"
            },
            {
              "name": "Gadget Instrument Flute",
              "technical": "DA_Item_Gadget_Instrument_Flute",
              "status": "unassessed"
            },
            {
              "name": "Gadget Instrument Harmonica",
              "technical": "DA_Item_Gadget_Instrument_Harmonica",
              "status": "unassessed"
            },
            {
              "name": "Gadget Instrument Shaker",
              "technical": "DA_Item_Gadget_Instrument_Shaker",
              "status": "unassessed"
            },
            {
              "name": "Grappling Hook",
              "technical": "DA_Item_GrapplingHook",
              "status": "unassessed"
            },
            {
              "name": "HEGrenade",
              "technical": "DA_Item_HEGrenade",
              "status": "unassessed"
            },
            {
              "name": "Healing Ho T Improvised",
              "technical": "DA_Item_HealingHoT_Improvised",
              "status": "unassessed"
            },
            {
              "name": "Healing Ho T Medium",
              "technical": "DA_Item_HealingHoT_Medium",
              "status": "unassessed"
            },
            {
              "name": "Healing Ho T Small",
              "technical": "DA_Item_HealingHoT_Small",
              "status": "unassessed"
            },
            {
              "name": "Health Cloud",
              "technical": "DA_Item_HealthCloud",
              "status": "unassessed"
            },
            {
              "name": "Health Pack 01",
              "technical": "DA_Item_HealthPack_01",
              "status": "unassessed"
            },
            {
              "name": "Health Spray",
              "technical": "DA_Item_HealthSpray",
              "status": "unassessed"
            },
            {
              "name": "Homing Missile",
              "technical": "DA_Item_HomingMissile",
              "status": "unassessed"
            },
            {
              "name": "Homing Missile Grenade",
              "technical": "DA_Item_HomingMissileGrenade",
              "status": "unassessed"
            },
            {
              "name": "Integrated Binoculars",
              "technical": "DA_Item_IntegratedBinoculars",
              "status": "unassessed"
            },
            {
              "name": "Integrated Defibrillator",
              "technical": "DA_Item_IntegratedDefibrillator",
              "status": "unassessed"
            },
            {
              "name": "Integrated Shield Repair",
              "technical": "DA_Item_IntegratedShieldRepair",
              "status": "unassessed"
            },
            {
              "name": "Item Bench 01",
              "technical": "DA_Item_ItemBench_01",
              "status": "unassessed"
            },
            {
              "name": "Item Bench 02",
              "technical": "DA_Item_ItemBench_02",
              "status": "unassessed"
            },
            {
              "name": "Item Bench 03",
              "technical": "DA_Item_ItemBench_03",
              "status": "unassessed"
            },
            {
              "name": "Jolt Mine",
              "technical": "DA_Item_JoltMine",
              "status": "unassessed"
            },
            {
              "name": "Jump Mine Impulse",
              "technical": "DA_Item_JumpMine_Impulse",
              "status": "unassessed"
            },
            {
              "name": "Jump Mine Stamina Do T",
              "technical": "DA_Item_JumpMine_StaminaDoT",
              "status": "unassessed"
            },
            {
              "name": "Key Item Raider Hatch Key",
              "technical": "DA_Item_KeyItem_RaiderHatchKey",
              "status": "unassessed"
            },
            {
              "name": "Laser Trap Do TGrenade",
              "technical": "DA_Item_LaserTrap_DoTGrenade",
              "status": "unassessed"
            },
            {
              "name": "Laser Trap Smoke Grenade",
              "technical": "DA_Item_LaserTrap_SmokeGrenade",
              "status": "unassessed"
            },
            {
              "name": "Laser Trap Stamina Do T",
              "technical": "DA_Item_LaserTrap_StaminaDoT",
              "status": "unassessed"
            },
            {
              "name": "Laser Trap Throwable Decoy",
              "technical": "DA_Item_LaserTrap_ThrowableDecoy",
              "status": "unassessed"
            },
            {
              "name": "Metal Detector 01",
              "technical": "DA_Item_MetalDetector_01",
              "status": "unassessed"
            },
            {
              "name": "Metal Detector 02",
              "technical": "DA_Item_MetalDetector_02",
              "status": "unassessed"
            },
            {
              "name": "Outpost Rooms Installed",
              "technical": "DA_Item_OutpostRoomsInstalled",
              "status": "unassessed"
            },
            {
              "name": "Potion Station 01",
              "technical": "DA_Item_PotionStation_01",
              "status": "unassessed"
            },
            {
              "name": "Powered Descender",
              "technical": "DA_Item_PoweredDescender",
              "status": "unassessed"
            },
            {
              "name": "Recycle Station 01",
              "technical": "DA_Item_RecycleStation_01",
              "status": "unassessed"
            },
            {
              "name": "Remote Trap Firework Box",
              "technical": "DA_Item_RemoteTrap_FireworkBox",
              "status": "unassessed"
            },
            {
              "name": "Resource Generator 01",
              "technical": "DA_Item_ResourceGenerator_01",
              "status": "unassessed"
            },
            {
              "name": "Resource Generator 02",
              "technical": "DA_Item_ResourceGenerator_02",
              "status": "unassessed"
            },
            {
              "name": "Resource Generator 03",
              "technical": "DA_Item_ResourceGenerator_03",
              "status": "unassessed"
            },
            {
              "name": "Resource Generator 04",
              "technical": "DA_Item_ResourceGenerator_04",
              "status": "unassessed"
            },
            {
              "name": "Resource Generator 05",
              "technical": "DA_Item_ResourceGenerator_05",
              "status": "unassessed"
            },
            {
              "name": "Scatter Missile Grenade",
              "technical": "DA_Item_ScatterMissileGrenade",
              "status": "unassessed"
            },
            {
              "name": "Smelter 01",
              "technical": "DA_Item_Smelter_01",
              "status": "unassessed"
            },
            {
              "name": "Smoke Grenade",
              "technical": "DA_Item_SmokeGrenade",
              "status": "unassessed"
            },
            {
              "name": "Snap Hook",
              "technical": "DA_Item_SnapHook",
              "status": "unassessed"
            },
            {
              "name": "Snowball",
              "technical": "DA_Item_Snowball",
              "status": "unassessed"
            },
            {
              "name": "Special Beam Rifle 01",
              "technical": "DA_Item_Special_BeamRifle_01",
              "status": "unassessed"
            },
            {
              "name": "Special Beam Rifle 01 Anti Weak Point",
              "technical": "DA_Item_Special_BeamRifle_01_AntiWeakPoint",
              "status": "unassessed"
            },
            {
              "name": "Special Beam Rifle 01 Anti Weak Point Bigger Mag",
              "technical": "DA_Item_Special_BeamRifle_01_AntiWeakPoint_BiggerMag",
              "status": "unassessed"
            },
            {
              "name": "Special Beam Rifle 01 Ascended",
              "technical": "DA_Item_Special_BeamRifle_01_Ascended",
              "status": "unassessed"
            },
            {
              "name": "Special Beam Rifle 01 Bigger Mag",
              "technical": "DA_Item_Special_BeamRifle_01_BiggerMag",
              "status": "unassessed"
            },
            {
              "name": "Special Beam Rifle 01 Ramping Damage",
              "technical": "DA_Item_Special_BeamRifle_01_RampingDamage",
              "status": "unassessed"
            },
            {
              "name": "Special Beam Rifle 01 Ramping Damage Bigger Mag",
              "technical": "DA_Item_Special_BeamRifle_01_RampingDamage_BiggerMag",
              "status": "unassessed"
            },
            {
              "name": "Special Railgun 01",
              "technical": "DA_Item_Special_Railgun_01",
              "status": "unassessed"
            },
            {
              "name": "Special Railgun 01 Ascended",
              "technical": "DA_Item_Special_Railgun_01_Ascended",
              "status": "unassessed"
            },
            {
              "name": "Special Railgun 01 Bigger Mag",
              "technical": "DA_Item_Special_Railgun_01_BiggerMag",
              "status": "unassessed"
            },
            {
              "name": "Special Railgun 01 Bigger Mag Scoped",
              "technical": "DA_Item_Special_Railgun_01_BiggerMag_Scoped",
              "status": "unassessed"
            },
            {
              "name": "Special Railgun 01 Bigger Mag Scoped Straight Bolt",
              "technical": "DA_Item_Special_Railgun_01_BiggerMag_Scoped_StraightBolt",
              "status": "unassessed"
            },
            {
              "name": "Special Railgun 01 Bigger Mag Straight Bolt",
              "technical": "DA_Item_Special_Railgun_01_BiggerMag_StraightBolt",
              "status": "unassessed"
            },
            {
              "name": "Special Railgun 01 Scoped",
              "technical": "DA_Item_Special_Railgun_01_Scoped",
              "status": "unassessed"
            },
            {
              "name": "Special Railgun 01 Scoped Straight Bolt",
              "technical": "DA_Item_Special_Railgun_01_Scoped_StraightBolt",
              "status": "unassessed"
            },
            {
              "name": "Special Railgun 01 Straight Bolt",
              "technical": "DA_Item_Special_Railgun_01_StraightBolt",
              "status": "unassessed"
            },
            {
              "name": "Spray Can Fire",
              "technical": "DA_Item_SprayCan_Fire",
              "status": "unassessed"
            },
            {
              "name": "Sticky Remote Grenade",
              "technical": "DA_Item_StickyRemoteGrenade",
              "status": "unassessed"
            },
            {
              "name": "Stun Grenade",
              "technical": "DA_Item_StunGrenade",
              "status": "unassessed"
            },
            {
              "name": "Tether Gun",
              "technical": "DA_Item_TetherGun",
              "status": "unassessed"
            },
            {
              "name": "Throwable Decoy",
              "technical": "DA_Item_ThrowableDecoy",
              "status": "unassessed"
            },
            {
              "name": "Throwable Glowstick Blue",
              "technical": "DA_Item_Throwable_Glowstick_Blue",
              "status": "unassessed"
            },
            {
              "name": "Throwable Glowstick Green",
              "technical": "DA_Item_Throwable_Glowstick_Green",
              "status": "unassessed"
            },
            {
              "name": "Throwable Glowstick Red",
              "technical": "DA_Item_Throwable_Glowstick_Red",
              "status": "unassessed"
            },
            {
              "name": "Throwable Glowstick Yellow",
              "technical": "DA_Item_Throwable_Glowstick_Yellow",
              "status": "unassessed"
            },
            {
              "name": "Throwable Impact Grenade Small",
              "technical": "DA_Item_Throwable_ImpactGrenade_Small",
              "status": "unassessed"
            },
            {
              "name": "Throwable Shrapnel Grenade",
              "technical": "DA_Item_Throwable_ShrapnelGrenade",
              "status": "unassessed"
            },
            {
              "name": "Throwable Smoke Grenade Small",
              "technical": "DA_Item_Throwable_SmokeGrenade_Small",
              "status": "unassessed"
            },
            {
              "name": "Throwable Stamina Do TGrenade",
              "technical": "DA_Item_Throwable_StaminaDoTGrenade",
              "status": "unassessed"
            },
            {
              "name": "Throwable Tagging Grenade",
              "technical": "DA_Item_Throwable_TaggingGrenade",
              "status": "unassessed"
            },
            {
              "name": "Throwable Tether Grenade",
              "technical": "DA_Item_Throwable_TetherGrenade",
              "status": "unassessed"
            },
            {
              "name": "Throwable Trailblazer",
              "technical": "DA_Item_Throwable_Trailblazer",
              "status": "unassessed"
            },
            {
              "name": "Throwable Volcanic Rock",
              "technical": "DA_Item_Throwable_VolcanicRock",
              "status": "unassessed"
            },
            {
              "name": "Timed Mine Explosive",
              "technical": "DA_Item_TimedMine_Explosive",
              "status": "unassessed"
            },
            {
              "name": "Transmitter",
              "technical": "DA_Item_Transmitter",
              "status": "unassessed"
            },
            {
              "name": "Unarmed",
              "technical": "DA_Item_Unarmed",
              "status": "unassessed"
            },
            {
              "name": "Weapon Bench 01",
              "technical": "DA_Item_WeaponBench_01",
              "status": "unassessed"
            },
            {
              "name": "Weapon Bench 02",
              "technical": "DA_Item_WeaponBench_02",
              "status": "unassessed"
            },
            {
              "name": "Weapon Bench 03",
              "technical": "DA_Item_WeaponBench_03",
              "status": "unassessed"
            },
            {
              "name": "White Flag",
              "technical": "DA_Item_WhiteFlag",
              "status": "unassessed"
            },
            {
              "name": "Zipline",
              "technical": "DA_Item_Zipline",
              "status": "unassessed"
            }
          ]
        },
        {
          "name": "Progression items",
          "children": [
            {
              "name": "Currency Base Reward Pass Points",
              "technical": "DA_Item_Currency_BaseRewardPassPoints",
              "status": "unassessed"
            },
            {
              "name": "Currency Battle Pass Points",
              "technical": "DA_Item_Currency_BattlePassPoints",
              "status": "unassessed"
            },
            {
              "name": "Currency Expedition Reward",
              "technical": "DA_Item_Currency_ExpeditionReward",
              "status": "unassessed"
            },
            {
              "name": "Currency Frozen Trail Reward Pass Points",
              "technical": "DA_Item_Currency_FrozenTrailRewardPassPoints",
              "status": "unassessed"
            },
            {
              "name": "Currency Hard",
              "technical": "DA_Item_Currency_Hard",
              "status": "unassessed"
            },
            {
              "name": "Currency Legacy Reward Pass Points",
              "technical": "DA_Item_Currency_LegacyRewardPassPoints",
              "status": "unassessed"
            },
            {
              "name": "Currency Live Event Currency",
              "technical": "DA_Item_Currency_LiveEventCurrency",
              "status": "unassessed"
            },
            {
              "name": "Currency Live Event03",
              "technical": "DA_Item_Currency_LiveEvent03",
              "status": "unassessed"
            },
            {
              "name": "Currency Live Event04",
              "technical": "DA_Item_Currency_LiveEvent04",
              "status": "unassessed"
            },
            {
              "name": "Currency Live Event05",
              "technical": "DA_Item_Currency_LiveEvent05",
              "status": "unassessed"
            },
            {
              "name": "Currency Live Event06",
              "technical": "DA_Item_Currency_LiveEvent06",
              "status": "unassessed"
            },
            {
              "name": "Currency Milestone Points",
              "technical": "DA_Item_Currency_MilestonePoints",
              "status": "unassessed"
            },
            {
              "name": "Currency Research Points",
              "technical": "DA_Item_Currency_ResearchPoints",
              "status": "unassessed"
            },
            {
              "name": "Currency Skill Points",
              "technical": "DA_Item_Currency_SkillPoints",
              "status": "unassessed"
            },
            {
              "name": "Currency Subway Promo A",
              "technical": "DA_Item_Currency_SubwayPromoA",
              "status": "unassessed"
            },
            {
              "name": "Currency Subway Promo B",
              "technical": "DA_Item_Currency_SubwayPromoB",
              "status": "unassessed"
            },
            {
              "name": "Currency Subway Promo C",
              "technical": "DA_Item_Currency_SubwayPromoC",
              "status": "unassessed"
            },
            {
              "name": "Currency Winter Event",
              "technical": "DA_Item_Currency_WinterEvent",
              "status": "unassessed"
            },
            {
              "name": "Grind Currency",
              "technical": "DA_Item_GrindCurrency",
              "status": "unassessed"
            },
            {
              "name": "Research Ascended Anti Shield",
              "technical": "DA_Item_Research_Ascended_AntiShield",
              "status": "unassessed"
            },
            {
              "name": "Research Ascended Anti Weak Point",
              "technical": "DA_Item_Research_Ascended_AntiWeakPoint",
              "status": "unassessed"
            },
            {
              "name": "Research Ascended Burst Fire",
              "technical": "DA_Item_Research_Ascended_BurstFire",
              "status": "unassessed"
            },
            {
              "name": "Research Ascended Charged Burst",
              "technical": "DA_Item_Research_Ascended_ChargedBurst",
              "status": "unassessed"
            },
            {
              "name": "Research Ascended Drum Mag",
              "technical": "DA_Item_Research_Ascended_DrumMag",
              "status": "unassessed"
            },
            {
              "name": "Research Ascended Incendiary Grenades",
              "technical": "DA_Item_Research_Ascended_IncendiaryGrenades",
              "status": "unassessed"
            },
            {
              "name": "Research Ascended Incendiary Rounds",
              "technical": "DA_Item_Research_Ascended_IncendiaryRounds",
              "status": "unassessed"
            },
            {
              "name": "Research Ascended Increased Burst",
              "technical": "DA_Item_Research_Ascended_IncreasedBurst",
              "status": "unassessed"
            },
            {
              "name": "Research Ascended Ramping Damage",
              "technical": "DA_Item_Research_Ascended_RampingDamage",
              "status": "unassessed"
            },
            {
              "name": "Research Ascended Scoped",
              "technical": "DA_Item_Research_Ascended_Scoped",
              "status": "unassessed"
            },
            {
              "name": "Research Ascended Semi Auto",
              "technical": "DA_Item_Research_Ascended_SemiAuto",
              "status": "unassessed"
            },
            {
              "name": "Research Ascended Slug Rounds",
              "technical": "DA_Item_Research_Ascended_SlugRounds",
              "status": "unassessed"
            },
            {
              "name": "Research Ascended Sprint Shooting",
              "technical": "DA_Item_Research_Ascended_SprintShooting",
              "status": "unassessed"
            },
            {
              "name": "Research Ascended Straight Bolt",
              "technical": "DA_Item_Research_Ascended_StraightBolt",
              "status": "unassessed"
            },
            {
              "name": "Research Ascended XRounds",
              "technical": "DA_Item_Research_Ascended_XRounds",
              "status": "unassessed"
            },
            {
              "name": "Rounds Extracted",
              "technical": "DA_Item_RoundsExtracted",
              "status": "unassessed"
            },
            {
              "name": "Rounds Played",
              "technical": "DA_Item_RoundsPlayed",
              "status": "unassessed"
            },
            {
              "name": "XP Points",
              "technical": "DA_Item_XP_Points",
              "status": "unassessed"
            }
          ]
        },
        {
          "name": "Shields",
          "children": [
            {
              "name": "Heavy Shield",
              "technical": "DA_Item_Armor_HeavyBody",
              "status": "pending",
              "note": "Shield gameplay support is not implemented."
            },
            {
              "name": "Light Shield",
              "technical": "DA_Item_Armor_LightBody",
              "status": "partial",
              "note": "Menu selection, Raid slot moves, initial shield health and weight work; damage absorption, depletion and repair remain."
            },
            {
              "name": "Medium Shield",
              "technical": "DA_Item_Armor_MediumBody",
              "status": "pending",
              "note": "Shield gameplay support is not implemented."
            }
          ]
        },
        {
          "name": "Salvage & resources",
          "children": [
            {
              "name": "Rubber Duck",
              "technical": "DA_Item_Salvage_Trinket_RubberDuck_Alien",
              "status": "partial",
              "note": "Stash/loadout persistence, Raid display and whole-stack moves work; the item comes home from a Dam extraction, and only Safe Pocket items survive surrender."
            },
            {
              "name": "Rubber Duck",
              "technical": "DA_Item_Salvage_Trinket_RubberDuck_Cow",
              "status": "partial",
              "note": "Stash/loadout persistence, Raid display and whole-stack moves work; the item comes home from a Dam extraction, and only Safe Pocket items survive surrender."
            },
            {
              "name": "Rubber Duck",
              "technical": "DA_Item_Salvage_Trinket_RubberDuck_Flamingo",
              "status": "partial",
              "note": "Stash/loadout persistence, Raid display and whole-stack moves work; the item comes home from a Dam extraction, and only Safe Pocket items survive surrender."
            },
            {
              "name": "Rubber Duck",
              "technical": "DA_Item_Salvage_Trinket_RubberDuck_Glitter",
              "status": "partial",
              "note": "Stash/loadout persistence, Raid display and whole-stack moves work; the item comes home from a Dam extraction, and only Safe Pocket items survive surrender."
            },
            {
              "name": "Rubber Duck",
              "technical": "DA_Item_Salvage_Trinket_RubberDuck_Lance",
              "status": "partial",
              "note": "Stash/loadout persistence, Raid display and whole-stack moves work; the item comes home from a Dam extraction, and only Safe Pocket items survive surrender."
            },
            {
              "name": "Rubber Duck",
              "technical": "DA_Item_Salvage_Trinket_RubberDuck_Scrappy",
              "status": "partial",
              "note": "Stash/loadout persistence, Raid display and whole-stack moves work; the item comes home from a Dam extraction, and only Safe Pocket items survive surrender."
            },
            {
              "name": "Rubber Duck",
              "technical": "DA_Item_Salvage_Trinket_RubberDuck_Standard",
              "status": "partial",
              "note": "Stash/loadout persistence, Raid display and whole-stack moves work; the item comes home from a Dam extraction, and only Safe Pocket items survive surrender."
            },
            {
              "name": "Rubber Duck",
              "technical": "DA_Item_Salvage_RubberDuck_Winner01",
              "status": "partial",
              "note": "Stash/loadout persistence, Raid display and whole-stack moves work; the item comes home from a Dam extraction, and only Safe Pocket items survive surrender."
            },
            {
              "name": "Rubber Duck",
              "technical": "DA_Item_Salvage_RubberDuck_Winner02",
              "status": "partial",
              "note": "Stash/loadout persistence, Raid display and whole-stack moves work; the item comes home from a Dam extraction, and only Safe Pocket items survive surrender."
            },
            {
              "name": "Rubber Duck",
              "technical": "DA_Item_Salvage_RubberDuck_Winner03",
              "status": "partial",
              "note": "Stash/loadout persistence, Raid display and whole-stack moves work; the item comes home from a Dam extraction, and only Safe Pocket items survive surrender."
            },
            {
              "name": "Salvage ARCMemory Card",
              "technical": "DA_Item_Salvage_ARCMemoryCard",
              "status": "unassessed"
            },
            {
              "name": "Salvage Accordion 01",
              "technical": "DA_Item_Salvage_Accordion_01",
              "status": "unassessed"
            },
            {
              "name": "Salvage Advanced ARCPowercell",
              "technical": "DA_Item_Salvage_AdvancedARCPowercell",
              "status": "unassessed"
            },
            {
              "name": "Salvage Advanced Electrical Components",
              "technical": "DA_Item_Salvage_AdvancedElectricalComponents",
              "status": "unassessed"
            },
            {
              "name": "Salvage Advanced Gun Parts",
              "technical": "DA_Item_Salvage_AdvancedGunParts",
              "status": "unassessed"
            },
            {
              "name": "Salvage Alarm Clock",
              "technical": "DA_Item_Salvage_AlarmClock",
              "status": "unassessed"
            },
            {
              "name": "Salvage Ancient Fort Security Code",
              "technical": "DA_Item_Salvage_AncientFortSecurityCode",
              "status": "unassessed"
            },
            {
              "name": "Salvage Anti Septic",
              "technical": "DA_Item_Salvage_AntiSeptic",
              "status": "unassessed"
            },
            {
              "name": "Salvage Arc Coolant 01",
              "technical": "DA_Item_Salvage_ArcCoolant_01",
              "status": "unassessed"
            },
            {
              "name": "Salvage Arc Impure Coolant 01",
              "technical": "DA_Item_Salvage_ArcImpureCoolant_01",
              "status": "unassessed"
            },
            {
              "name": "Salvage Archive Desk Lamp",
              "technical": "DA_Item_Salvage_ArchiveDeskLamp",
              "status": "unassessed"
            },
            {
              "name": "Salvage Ascension Fragments",
              "technical": "DA_Item_Salvage_AscensionFragments",
              "status": "unassessed"
            },
            {
              "name": "Salvage Ascension Material1",
              "technical": "DA_Item_Salvage_AscensionMaterial1",
              "status": "unassessed"
            },
            {
              "name": "Salvage Ascension Material2",
              "technical": "DA_Item_Salvage_AscensionMaterial2",
              "status": "unassessed"
            },
            {
              "name": "Salvage Ascension Material3",
              "technical": "DA_Item_Salvage_AscensionMaterial3",
              "status": "unassessed"
            },
            {
              "name": "Salvage Ascension Material4",
              "technical": "DA_Item_Salvage_AscensionMaterial4",
              "status": "unassessed"
            },
            {
              "name": "Salvage Baton 01",
              "technical": "DA_Item_Salvage_Baton_01",
              "status": "unassessed"
            },
            {
              "name": "Salvage Battery",
              "technical": "DA_Item_Salvage_Battery",
              "status": "unassessed"
            },
            {
              "name": "Salvage Bicycle Pump 01",
              "technical": "DA_Item_Salvage_BicyclePump_01",
              "status": "unassessed"
            },
            {
              "name": "Salvage Blue Gate Utility Key1",
              "technical": "DA_Item_Salvage_BlueGateUtilityKey1",
              "status": "unassessed"
            },
            {
              "name": "Salvage Blue Gate Utility Key2",
              "technical": "DA_Item_Salvage_BlueGateUtilityKey2",
              "status": "unassessed"
            },
            {
              "name": "Salvage Blue Gate Utility Key3",
              "technical": "DA_Item_Salvage_BlueGateUtilityKey3",
              "status": "unassessed"
            },
            {
              "name": "Salvage Blue Gate Utility Key4",
              "technical": "DA_Item_Salvage_BlueGateUtilityKey4",
              "status": "unassessed"
            },
            {
              "name": "Salvage Broken ARCMechanism",
              "technical": "DA_Item_Salvage_BrokenARCMechanism",
              "status": "unassessed"
            },
            {
              "name": "Salvage Broken Transmitter",
              "technical": "DA_Item_Salvage_BrokenTransmitter",
              "status": "unassessed"
            },
            {
              "name": "Salvage Bronze Mask Fragment 01",
              "technical": "DA_Item_Salvage_BronzeMaskFragment_01",
              "status": "unassessed"
            },
            {
              "name": "Salvage Bronze Statuette 01",
              "technical": "DA_Item_Salvage_BronzeStatuette_01",
              "status": "unassessed"
            },
            {
              "name": "Salvage Bureau Desk Lamp",
              "technical": "DA_Item_Salvage_BureauDeskLamp",
              "status": "unassessed"
            },
            {
              "name": "Salvage Buried City Utility Key1",
              "technical": "DA_Item_Salvage_BuriedCityUtilityKey1",
              "status": "unassessed"
            },
            {
              "name": "Salvage Buried City Utility Key2",
              "technical": "DA_Item_Salvage_BuriedCityUtilityKey2",
              "status": "unassessed"
            },
            {
              "name": "Salvage Buried City Utility Key3",
              "technical": "DA_Item_Salvage_BuriedCityUtilityKey3",
              "status": "unassessed"
            },
            {
              "name": "Salvage Buried City Utility Key4",
              "technical": "DA_Item_Salvage_BuriedCityUtilityKey4",
              "status": "unassessed"
            },
            {
              "name": "Salvage Camera Lens 01",
              "technical": "DA_Item_Salvage_CameraLens_01",
              "status": "unassessed"
            },
            {
              "name": "Salvage Candle Holder",
              "technical": "DA_Item_Salvage_CandleHolder",
              "status": "unassessed"
            },
            {
              "name": "Salvage Canister",
              "technical": "DA_Item_Salvage_Canister",
              "status": "unassessed"
            },
            {
              "name": "Salvage Cat Bed 01",
              "technical": "DA_Item_Salvage_CatBed_01",
              "status": "unassessed"
            },
            {
              "name": "Salvage Chemicals",
              "technical": "DA_Item_Salvage_Chemicals",
              "status": "unassessed"
            },
            {
              "name": "Salvage Chonk Part",
              "technical": "DA_Item_Salvage_Chonk_Part",
              "status": "unassessed"
            },
            {
              "name": "Salvage Collector Key 01",
              "technical": "DA_Item_Salvage_CollectorKey_01",
              "status": "unassessed"
            },
            {
              "name": "Salvage Collector Key Component 01",
              "technical": "DA_Item_Salvage_CollectorKeyComponent_01",
              "status": "unassessed"
            },
            {
              "name": "Salvage Collector Key Component 02",
              "technical": "DA_Item_Salvage_CollectorKeyComponent_02",
              "status": "unassessed"
            },
            {
              "name": "Salvage Collector Key Component 03",
              "technical": "DA_Item_Salvage_CollectorKeyComponent_03",
              "status": "unassessed"
            },
            {
              "name": "Salvage Collector1",
              "technical": "DA_Item_Salvage_Collector1",
              "status": "unassessed"
            },
            {
              "name": "Salvage Comet Part",
              "technical": "DA_Item_Salvage_CometPart",
              "status": "unassessed"
            },
            {
              "name": "Salvage Comfortable Pillow 01",
              "technical": "DA_Item_Salvage_ComfortablePillow_01",
              "status": "unassessed"
            },
            {
              "name": "Salvage Common Ascension Module",
              "technical": "DA_Item_Salvage_CommonAscensionModule",
              "status": "unassessed"
            },
            {
              "name": "Salvage Community Event TT1 Bunker Key",
              "technical": "DA_Item_Salvage_Community_Event_TT1_Bunker_Key",
              "status": "unassessed"
            },
            {
              "name": "Salvage Complex Gun Parts",
              "technical": "DA_Item_Salvage_ComplexGunParts",
              "status": "unassessed"
            },
            {
              "name": "Salvage Coolant",
              "technical": "DA_Item_Salvage_Coolant",
              "status": "unassessed"
            },
            {
              "name": "Salvage Cooling Coil",
              "technical": "DA_Item_Salvage_CoolingCoil",
              "status": "unassessed"
            },
            {
              "name": "Salvage Cooling Fan",
              "technical": "DA_Item_Salvage_CoolingFan",
              "status": "unassessed"
            },
            {
              "name": "Salvage Corrupted ARC Part 01",
              "technical": "DA_Item_Salvage_Corrupted_ARC_Part_01",
              "status": "unassessed"
            },
            {
              "name": "Salvage Corrupted ARC Part 02",
              "technical": "DA_Item_Salvage_Corrupted_ARC_Part_02",
              "status": "unassessed"
            },
            {
              "name": "Salvage Corrupted ARC Part 03",
              "technical": "DA_Item_Salvage_Corrupted_ARC_Part_03",
              "status": "unassessed"
            },
            {
              "name": "Salvage Corrupted ARC Part 04",
              "technical": "DA_Item_Salvage_Corrupted_ARC_Part_04",
              "status": "unassessed"
            },
            {
              "name": "Salvage Corrupted ARC Part 05",
              "technical": "DA_Item_Salvage_Corrupted_ARC_Part_05",
              "status": "unassessed"
            },
            {
              "name": "Salvage Crumpled Plastic Bottle",
              "technical": "DA_Item_Salvage_CrumpledPlasticBottle",
              "status": "unassessed"
            },
            {
              "name": "Salvage Dam Controlled Access Zone Key",
              "technical": "DA_Item_Salvage_DamControlledAccessZoneKey",
              "status": "unassessed"
            },
            {
              "name": "Salvage Damaged Heat Sink",
              "technical": "DA_Item_Salvage_DamagedHeatSink",
              "status": "unassessed"
            },
            {
              "name": "Salvage Dart Board 01",
              "technical": "DA_Item_Salvage_DartBoard_01",
              "status": "unassessed"
            },
            {
              "name": "Salvage Dead Flower Bouquet 01",
              "technical": "DA_Item_Salvage_DeadFlowerBouquet_01",
              "status": "unassessed"
            },
            {
              "name": "Salvage Desk Lamp",
              "technical": "DA_Item_Salvage_DeskLamp",
              "status": "unassessed"
            },
            {
              "name": "Salvage Diving Goggles 01",
              "technical": "DA_Item_Salvage_DivingGoggles_01",
              "status": "unassessed"
            },
            {
              "name": "Salvage Duct Tape",
              "technical": "DA_Item_Salvage_DuctTape",
              "status": "unassessed"
            },
            {
              "name": "Salvage Electrical Components",
              "technical": "DA_Item_Salvage_ElectricalComponents",
              "status": "unassessed"
            },
            {
              "name": "Salvage Elephant Obelisk",
              "technical": "DA_Item_Salvage_ElephantObelisk",
              "status": "unassessed"
            },
            {
              "name": "Salvage Epic Ascension Module",
              "technical": "DA_Item_Salvage_EpicAscensionModule",
              "status": "unassessed"
            },
            {
              "name": "Salvage Equatorial Sundial",
              "technical": "DA_Item_Salvage_EquatorialSundial",
              "status": "unassessed"
            },
            {
              "name": "Salvage Event Item Candleberries",
              "technical": "DA_Item_Salvage_Event_Item_Candleberries",
              "status": "unassessed"
            },
            {
              "name": "Salvage Exodus Book",
              "technical": "DA_Item_Salvage_ExodusBook",
              "status": "unassessed"
            },
            {
              "name": "Salvage Exodus Prototype",
              "technical": "DA_Item_Salvage_ExodusPrototype",
              "status": "unassessed"
            },
            {
              "name": "Salvage Expired Security Code",
              "technical": "DA_Item_Salvage_ExpiredSecurityCode",
              "status": "unassessed"
            },
            {
              "name": "Salvage Explosive Mixture",
              "technical": "DA_Item_Salvage_ExplosiveMixture",
              "status": "unassessed"
            },
            {
              "name": "Salvage Extractor Part",
              "technical": "DA_Item_Salvage_Extractor_Part",
              "status": "unassessed"
            },
            {
              "name": "Salvage Fabric",
              "technical": "DA_Item_Salvage_Fabric",
              "status": "unassessed"
            },
            {
              "name": "Salvage Fertilizer",
              "technical": "DA_Item_Salvage_Fertilizer",
              "status": "unassessed"
            },
            {
              "name": "Salvage Film Reel 01",
              "technical": "DA_Item_Salvage_FilmReel_01",
              "status": "unassessed"
            },
            {
              "name": "Salvage Fine Wristwatch 01",
              "technical": "DA_Item_Salvage_FineWristwatch_01",
              "status": "unassessed"
            },
            {
              "name": "Salvage Fireball Part",
              "technical": "DA_Item_Salvage_FireballPart",
              "status": "unassessed"
            },
            {
              "name": "Salvage Flashlight Broken 01",
              "technical": "DA_Item_Salvage_FlashlightBroken_01",
              "status": "unassessed"
            },
            {
              "name": "Salvage Flow Controller",
              "technical": "DA_Item_Salvage_FlowController",
              "status": "unassessed"
            },
            {
              "name": "Salvage Football Shoes 01 Green",
              "technical": "DA_Item_Salvage_FootballShoes_01_Green",
              "status": "unassessed"
            },
            {
              "name": "Salvage Football Shoes 01 Red",
              "technical": "DA_Item_Salvage_FootballShoes_01_Red",
              "status": "unassessed"
            },
            {
              "name": "Salvage Football Shoes 01 Silver",
              "technical": "DA_Item_Salvage_FootballShoes_01_Silver",
              "status": "unassessed"
            },
            {
              "name": "Salvage Fossilized Lightning",
              "technical": "DA_Item_Salvage_FossilizedLightning",
              "status": "unassessed"
            },
            {
              "name": "Salvage Frequency Modulation Box",
              "technical": "DA_Item_Salvage_FrequencyModulationBox",
              "status": "unassessed"
            },
            {
              "name": "Salvage Fried Motherboard",
              "technical": "DA_Item_Salvage_FriedMotherboard",
              "status": "unassessed"
            },
            {
              "name": "Salvage Frigate Memory Module",
              "technical": "DA_Item_Salvage_FrigateMemoryModule",
              "status": "unassessed"
            },
            {
              "name": "Salvage Frigate1",
              "technical": "DA_Item_Salvage_Frigate1",
              "status": "unassessed"
            },
            {
              "name": "Salvage Frozen Trail Mat1",
              "technical": "DA_Item_Salvage_FrozenTrailMat1",
              "status": "unassessed"
            },
            {
              "name": "Salvage Frozen Trail Mat2",
              "technical": "DA_Item_Salvage_FrozenTrailMat2",
              "status": "unassessed"
            },
            {
              "name": "Salvage Frozen Trail Mat3",
              "technical": "DA_Item_Salvage_FrozenTrailMat3",
              "status": "unassessed"
            },
            {
              "name": "Salvage Frozen Trail Mechanical1",
              "technical": "DA_Item_Salvage_FrozenTrailMechanical1",
              "status": "unassessed"
            },
            {
              "name": "Salvage Frozen Trail Mechanical2",
              "technical": "DA_Item_Salvage_FrozenTrailMechanical2",
              "status": "unassessed"
            },
            {
              "name": "Salvage Frozen Trail Mechanical3",
              "technical": "DA_Item_Salvage_FrozenTrailMechanical3",
              "status": "unassessed"
            },
            {
              "name": "Salvage Frozen Trail Mechanical4",
              "technical": "DA_Item_Salvage_FrozenTrailMechanical4",
              "status": "unassessed"
            },
            {
              "name": "Salvage Frozen Trail Observatory Key 01",
              "technical": "DA_Item_Salvage_FrozenTrailObservatoryKey_01",
              "status": "unassessed"
            },
            {
              "name": "Salvage Frozen Trail Rail Yard Key 01",
              "technical": "DA_Item_Salvage_FrozenTrailRailYardKey_01",
              "status": "unassessed"
            },
            {
              "name": "Salvage Frozen Trail Station Key 01",
              "technical": "DA_Item_Salvage_FrozenTrailStationKey_01",
              "status": "unassessed"
            },
            {
              "name": "Salvage Frozen Trail Tech1",
              "technical": "DA_Item_Salvage_FrozenTrailTech1",
              "status": "unassessed"
            },
            {
              "name": "Salvage Frozen Trail Tech2",
              "technical": "DA_Item_Salvage_FrozenTrailTech2",
              "status": "unassessed"
            },
            {
              "name": "Salvage Frozen Trail Tech3",
              "technical": "DA_Item_Salvage_FrozenTrailTech3",
              "status": "unassessed"
            },
            {
              "name": "Salvage Frozen Trail Tech4",
              "technical": "DA_Item_Salvage_FrozenTrailTech4",
              "status": "unassessed"
            },
            {
              "name": "Salvage Frozen Trail Village Key",
              "technical": "DA_Item_Salvage_FrozenTrailVillageKey",
              "status": "unassessed"
            },
            {
              "name": "Salvage Frying Pan",
              "technical": "DA_Item_Salvage_FryingPan",
              "status": "unassessed"
            },
            {
              "name": "Salvage Geiger Counter",
              "technical": "DA_Item_Salvage_GeigerCounter",
              "status": "unassessed"
            },
            {
              "name": "Salvage Guidance System 01",
              "technical": "DA_Item_Salvage_GuidanceSystem_01",
              "status": "unassessed"
            },
            {
              "name": "Salvage Gun Parts",
              "technical": "DA_Item_Salvage_GunParts",
              "status": "unassessed"
            },
            {
              "name": "Salvage Handcuffs",
              "technical": "DA_Item_Salvage_Handcuffs",
              "status": "unassessed"
            },
            {
              "name": "Salvage Headphones",
              "technical": "DA_Item_Salvage_Headphones",
              "status": "unassessed"
            },
            {
              "name": "Salvage Heavy Drone Laser Part",
              "technical": "DA_Item_Salvage_HeavyDroneLaser_Part",
              "status": "unassessed"
            },
            {
              "name": "Salvage Hornetflamethrower Part",
              "technical": "DA_Item_Salvage_Hornetflamethrower_Part",
              "status": "unassessed"
            },
            {
              "name": "Salvage Household Cleaner",
              "technical": "DA_Item_Salvage_HouseholdCleaner",
              "status": "unassessed"
            },
            {
              "name": "Salvage Humidifier 01",
              "technical": "DA_Item_Salvage_Humidifier_01",
              "status": "unassessed"
            },
            {
              "name": "Salvage Industrial Battery",
              "technical": "DA_Item_Salvage_IndustrialBattery",
              "status": "unassessed"
            },
            {
              "name": "Salvage Industrial Charger",
              "technical": "DA_Item_Salvage_IndustrialCharger",
              "status": "unassessed"
            },
            {
              "name": "Salvage Industrial Magnet",
              "technical": "DA_Item_Salvage_IndustrialMagnet",
              "status": "unassessed"
            },
            {
              "name": "Salvage Ion Sputter",
              "technical": "DA_Item_Salvage_IonSputter",
              "status": "unassessed"
            },
            {
              "name": "Salvage Laboratory Reagents",
              "technical": "DA_Item_Salvage_LaboratoryReagents",
              "status": "unassessed"
            },
            {
              "name": "Salvage Legendary Ascension Module",
              "technical": "DA_Item_Salvage_LegendaryAscensionModule",
              "status": "unassessed"
            },
            {
              "name": "Salvage Light Gun Parts",
              "technical": "DA_Item_Salvage_LightGunParts",
              "status": "unassessed"
            },
            {
              "name": "Salvage Magnet",
              "technical": "DA_Item_Salvage_Magnet",
              "status": "unassessed"
            },
            {
              "name": "Salvage Magnetic Accelerator",
              "technical": "DA_Item_Salvage_MagneticAccelerator",
              "status": "unassessed"
            },
            {
              "name": "Salvage Magnetron",
              "technical": "DA_Item_Salvage_Magnetron",
              "status": "unassessed"
            },
            {
              "name": "Salvage Majolica Tazza 01",
              "technical": "DA_Item_Salvage_MajolicaTazza_01",
              "status": "unassessed"
            },
            {
              "name": "Salvage Mat ARC Core",
              "technical": "DA_Item_Salvage_Mat_ARC_Core",
              "status": "unassessed"
            },
            {
              "name": "Salvage Mat ARC Metal",
              "technical": "DA_Item_Salvage_Mat_ARC_Metal",
              "status": "unassessed"
            },
            {
              "name": "Salvage Mat Arc Circuit Board",
              "technical": "DA_Item_Salvage_Mat_ArcCircuitBoard",
              "status": "unassessed"
            },
            {
              "name": "Salvage Mat Arc Mechanism",
              "technical": "DA_Item_Salvage_Mat_ArcMechanism",
              "status": "unassessed"
            },
            {
              "name": "Salvage Mat Baron Cloth",
              "technical": "DA_Item_Salvage_Mat_Baron_Cloth",
              "status": "unassessed"
            },
            {
              "name": "Salvage Mat Baron Ingot",
              "technical": "DA_Item_Salvage_Mat_Baron_Ingot",
              "status": "unassessed"
            },
            {
              "name": "Salvage Mat Baron Oil",
              "technical": "DA_Item_Salvage_Mat_Baron_Oil",
              "status": "unassessed"
            },
            {
              "name": "Salvage Mat Baron Rubber",
              "technical": "DA_Item_Salvage_Mat_Baron_Rubber",
              "status": "unassessed"
            },
            {
              "name": "Salvage Mat Bison Part",
              "technical": "DA_Item_Salvage_Mat_Bison_Part",
              "status": "unassessed"
            },
            {
              "name": "Salvage Mat Burned ARC Circuit Board",
              "technical": "DA_Item_Salvage_Mat_Burned_ARC_CircuitBoard",
              "status": "unassessed"
            },
            {
              "name": "Salvage Mat Damage Rocketeer Part",
              "technical": "DA_Item_Salvage_Mat_Damage_Rocketeer_Part",
              "status": "unassessed"
            },
            {
              "name": "Salvage Mat Damaged ARC Core",
              "technical": "DA_Item_Salvage_Mat_Damaged_ARC_Core",
              "status": "unassessed"
            },
            {
              "name": "Salvage Mat Damaged Bison Part",
              "technical": "DA_Item_Salvage_Mat_Damaged_Bison_Part",
              "status": "unassessed"
            },
            {
              "name": "Salvage Mat Damaged Hornet Part",
              "technical": "DA_Item_Salvage_Mat_Damaged_Hornet_Part",
              "status": "unassessed"
            },
            {
              "name": "Salvage Mat Damaged Snitch Scanner",
              "technical": "DA_Item_Salvage_Mat_Damaged_Snitch_Scanner",
              "status": "unassessed"
            },
            {
              "name": "Salvage Mat Damaged Wasp Part",
              "technical": "DA_Item_Salvage_Mat_Damaged_Wasp_Part",
              "status": "unassessed"
            },
            {
              "name": "Salvage Mat Heavy Drone Part",
              "technical": "DA_Item_Salvage_Mat_HeavyDronePart",
              "status": "unassessed"
            },
            {
              "name": "Salvage Mat Heavy Gun Parts",
              "technical": "DA_Item_Salvage_Mat_HeavyGunParts",
              "status": "unassessed"
            },
            {
              "name": "Salvage Mat Hornet Part",
              "technical": "DA_Item_Salvage_Mat_Hornet_Part",
              "status": "unassessed"
            },
            {
              "name": "Salvage Mat Intact Baron Cloth",
              "technical": "DA_Item_Salvage_Mat_Intact_Baron_Cloth",
              "status": "unassessed"
            },
            {
              "name": "Salvage Mat Intact Baron Ingot",
              "technical": "DA_Item_Salvage_Mat_Intact_Baron_Ingot",
              "status": "unassessed"
            },
            {
              "name": "Salvage Mat Intact Baron Oil",
              "technical": "DA_Item_Salvage_Mat_Intact_Baron_Oil",
              "status": "unassessed"
            },
            {
              "name": "Salvage Mat Intact Baron Rubber",
              "technical": "DA_Item_Salvage_Mat_Intact_Baron_Rubber",
              "status": "unassessed"
            },
            {
              "name": "Salvage Mat Junk 2",
              "technical": "DA_Item_Salvage_Mat_Junk_2",
              "status": "unassessed"
            },
            {
              "name": "Salvage Mat Junk 3",
              "technical": "DA_Item_Salvage_Mat_Junk_3",
              "status": "unassessed"
            },
            {
              "name": "Salvage Mat Roll Bot Damaged Part",
              "technical": "DA_Item_Salvage_Mat_RollBot_Damaged_Part",
              "status": "unassessed"
            },
            {
              "name": "Salvage Mat Snitch Part",
              "technical": "DA_Item_Salvage_Mat_Snitch_Part",
              "status": "unassessed"
            },
            {
              "name": "Salvage Mat Tick Damaged Part",
              "technical": "DA_Item_Salvage_Mat_Tick_Damaged_Part",
              "status": "unassessed"
            },
            {
              "name": "Salvage Mat Tick Part",
              "technical": "DA_Item_Salvage_Mat_Tick_Part",
              "status": "unassessed"
            },
            {
              "name": "Salvage Mat Wasp Part",
              "technical": "DA_Item_Salvage_Mat_Wasp_Part",
              "status": "unassessed"
            },
            {
              "name": "Salvage Mechanical Components",
              "technical": "DA_Item_Salvage_MechanicalComponents",
              "status": "unassessed"
            },
            {
              "name": "Salvage Medical Kit Broken 01",
              "technical": "DA_Item_Salvage_MedicalKitBroken_01",
              "status": "unassessed"
            },
            {
              "name": "Salvage Medium Gun Parts",
              "technical": "DA_Item_Salvage_MediumGunParts",
              "status": "unassessed"
            },
            {
              "name": "Salvage Meerkat Part",
              "technical": "DA_Item_Salvage_Meerkat_Part",
              "status": "unassessed"
            },
            {
              "name": "Salvage Meerkat Part Turret",
              "technical": "DA_Item_Salvage_Meerkat_Part_Turret",
              "status": "unassessed"
            },
            {
              "name": "Salvage Metal Brackets",
              "technical": "DA_Item_Salvage_MetalBrackets",
              "status": "unassessed"
            },
            {
              "name": "Salvage Microscope",
              "technical": "DA_Item_Salvage_Microscope",
              "status": "unassessed"
            },
            {
              "name": "Salvage Mini Centrifuge",
              "technical": "DA_Item_Salvage_MiniCentrifuge",
              "status": "unassessed"
            },
            {
              "name": "Salvage Mod Components",
              "technical": "DA_Item_Salvage_ModComponents",
              "status": "unassessed"
            },
            {
              "name": "Salvage Monk Part",
              "technical": "DA_Item_Salvage_MonkPart",
              "status": "unassessed"
            },
            {
              "name": "Salvage Monolith Part",
              "technical": "DA_Item_Salvage_Monolith_Part",
              "status": "unassessed"
            },
            {
              "name": "Salvage Motor",
              "technical": "DA_Item_Salvage_Motor",
              "status": "unassessed"
            },
            {
              "name": "Salvage Mountain Compound Utility Key1",
              "technical": "DA_Item_Salvage_MountainCompound_UtilityKey1",
              "status": "unassessed"
            },
            {
              "name": "Salvage Mountain Compound Utility Key3",
              "technical": "DA_Item_Salvage_MountainCompound_UtilityKey3",
              "status": "unassessed"
            },
            {
              "name": "Salvage Mountain Compound Utility Key4",
              "technical": "DA_Item_Salvage_MountainCompound_UtilityKey4",
              "status": "unassessed"
            },
            {
              "name": "Salvage Mountain Compound Utilitykey2",
              "technical": "DA_Item_Salvage_MountainCompound_Utilitykey2",
              "status": "unassessed"
            },
            {
              "name": "Salvage Music Album 01",
              "technical": "DA_Item_Salvage_MusicAlbum_01",
              "status": "unassessed"
            },
            {
              "name": "Salvage Music Box 01",
              "technical": "DA_Item_Salvage_MusicBox_01",
              "status": "unassessed"
            },
            {
              "name": "Salvage Number Plate",
              "technical": "DA_Item_Salvage_NumberPlate",
              "status": "unassessed"
            },
            {
              "name": "Salvage Oil",
              "technical": "DA_Item_Salvage_Oil",
              "status": "unassessed"
            },
            {
              "name": "Salvage Old Book",
              "technical": "DA_Item_Salvage_OldBook",
              "status": "unassessed"
            },
            {
              "name": "Salvage Paint",
              "technical": "DA_Item_Salvage_Paint",
              "status": "unassessed"
            },
            {
              "name": "Salvage Painted Box 01",
              "technical": "DA_Item_Salvage_PaintedBox_01",
              "status": "unassessed"
            },
            {
              "name": "Salvage Parachute 01",
              "technical": "DA_Item_Salvage_Parachute_01",
              "status": "unassessed"
            },
            {
              "name": "Salvage Patrol Car Key",
              "technical": "DA_Item_Salvage_PatrolCarKey",
              "status": "unassessed"
            },
            {
              "name": "Salvage Peppermint Part",
              "technical": "DA_Item_Salvage_Peppermint_Part",
              "status": "unassessed"
            },
            {
              "name": "Salvage Pilgrims Peak Security Code",
              "technical": "DA_Item_Salvage_PilgrimsPeakSecurityCode",
              "status": "unassessed"
            },
            {
              "name": "Salvage Pinger Part",
              "technical": "DA_Item_Salvage_PingerPart",
              "status": "unassessed"
            },
            {
              "name": "Salvage Planks",
              "technical": "DA_Item_Salvage_Planks",
              "status": "unassessed"
            },
            {
              "name": "Salvage Plastic",
              "technical": "DA_Item_Salvage_Plastic",
              "status": "unassessed"
            },
            {
              "name": "Salvage Plastic Trash",
              "technical": "DA_Item_Salvage_PlasticTrash",
              "status": "unassessed"
            },
            {
              "name": "Salvage Polluted Air Filter",
              "technical": "DA_Item_Salvage_PollutedAirFilter",
              "status": "unassessed"
            },
            {
              "name": "Salvage Pop Part",
              "technical": "DA_Item_Salvage_PopPart",
              "status": "unassessed"
            },
            {
              "name": "Salvage Portable TV",
              "technical": "DA_Item_Salvage_PortableTV",
              "status": "unassessed"
            },
            {
              "name": "Salvage Poster Nature 01",
              "technical": "DA_Item_Salvage_PosterNature_01",
              "status": "unassessed"
            },
            {
              "name": "Salvage Potted Plant",
              "technical": "DA_Item_Salvage_PottedPlant",
              "status": "unassessed"
            },
            {
              "name": "Salvage Pottery 01",
              "technical": "DA_Item_Salvage_Pottery_01",
              "status": "unassessed"
            },
            {
              "name": "Salvage Power Bank",
              "technical": "DA_Item_Salvage_PowerBank",
              "status": "unassessed"
            },
            {
              "name": "Salvage Power Cable",
              "technical": "DA_Item_Salvage_PowerCable",
              "status": "unassessed"
            },
            {
              "name": "Salvage Power Rod",
              "technical": "DA_Item_Salvage_PowerRod",
              "status": "unassessed"
            },
            {
              "name": "Salvage Processor",
              "technical": "DA_Item_Salvage_Processor",
              "status": "unassessed"
            },
            {
              "name": "Salvage Projector 01",
              "technical": "DA_Item_Salvage_Projector_01",
              "status": "unassessed"
            },
            {
              "name": "Salvage Purification System Blueprint",
              "technical": "DA_Item_Salvage_PurificationSystemBlueprint",
              "status": "unassessed"
            },
            {
              "name": "Salvage Quest Aivas Patch",
              "technical": "DA_Item_Salvage_Quest_AivasPatch",
              "status": "unassessed"
            },
            {
              "name": "Salvage Quest Broken Compass",
              "technical": "DA_Item_Salvage_Quest_BrokenCompass",
              "status": "unassessed"
            },
            {
              "name": "Salvage Quest Celeste Journal",
              "technical": "DA_Item_Salvage_Quest_CelesteJournal",
              "status": "unassessed"
            },
            {
              "name": "Salvage Quest Collector Circuitry",
              "technical": "DA_Item_Salvage_Quest_CollectorCircuitry",
              "status": "unassessed"
            },
            {
              "name": "Salvage Quest Dodgers Note",
              "technical": "DA_Item_Salvage_Quest_DodgersNote",
              "status": "unassessed"
            },
            {
              "name": "Salvage Quest ECMeter",
              "technical": "DA_Item_Salvage_Quest_ECMeter",
              "status": "unassessed"
            },
            {
              "name": "Salvage Quest ESRAnalyzer",
              "technical": "DA_Item_Salvage_Quest_ESRAnalyzer",
              "status": "unassessed"
            },
            {
              "name": "Salvage Quest Espresso Machine Parts",
              "technical": "DA_Item_Salvage_Quest_EspressoMachineParts",
              "status": "unassessed"
            },
            {
              "name": "Salvage Quest First Wave Camcorder",
              "technical": "DA_Item_Salvage_Quest_FirstWaveCamcorder",
              "status": "unassessed"
            },
            {
              "name": "Salvage Quest First Wave Compass",
              "technical": "DA_Item_Salvage_Quest_FirstWaveCompass",
              "status": "unassessed"
            },
            {
              "name": "Salvage Quest First Wave Rations",
              "technical": "DA_Item_Salvage_Quest_FirstWaveRations",
              "status": "unassessed"
            },
            {
              "name": "Salvage Quest Li DARScanner",
              "technical": "DA_Item_Salvage_Quest_LiDARScanner",
              "status": "unassessed"
            },
            {
              "name": "Salvage Quest Library Book",
              "technical": "DA_Item_Salvage_Quest_LibraryBook",
              "status": "unassessed"
            },
            {
              "name": "Salvage Quest Mementos",
              "technical": "DA_Item_Salvage_Quest_Mementos",
              "status": "unassessed"
            },
            {
              "name": "Salvage Quest Moisture Probe",
              "technical": "DA_Item_Salvage_Quest_Moisture_Probe",
              "status": "unassessed"
            },
            {
              "name": "Salvage Quest Nomad Tech Item",
              "technical": "DA_Item_Salvage_Quest_NomadTechItem",
              "status": "unassessed"
            },
            {
              "name": "Salvage Quest Official Shutdown Documentation",
              "technical": "DA_Item_Salvage_Quest_Official_Shutdown_Documentation",
              "status": "unassessed"
            },
            {
              "name": "Salvage Quest Old Market Card",
              "technical": "DA_Item_Salvage_Quest_OldMarket_Card",
              "status": "unassessed"
            },
            {
              "name": "Salvage Quest Old Report",
              "technical": "DA_Item_Salvage_Quest_OldReport",
              "status": "unassessed"
            },
            {
              "name": "Salvage Quest Plant",
              "technical": "DA_Item_Salvage_Quest_Plant",
              "status": "unassessed"
            },
            {
              "name": "Salvage Quest Precious Cargo",
              "technical": "DA_Item_Salvage_Quest_PreciousCargo",
              "status": "unassessed"
            },
            {
              "name": "Salvage Quest Precision Gimbal",
              "technical": "DA_Item_Salvage_Quest_PrecisionGimbal",
              "status": "unassessed"
            },
            {
              "name": "Salvage Quest Raider Flag",
              "technical": "DA_Item_Salvage_Quest_RaiderFlag",
              "status": "unassessed"
            },
            {
              "name": "Salvage Quest Rare Books",
              "technical": "DA_Item_Salvage_Quest_RareBooks",
              "status": "unassessed"
            },
            {
              "name": "Salvage Quest Recording Tape",
              "technical": "DA_Item_Salvage_Quest_RecordingTape",
              "status": "unassessed"
            },
            {
              "name": "Salvage Quest Scout Patrol Note",
              "technical": "DA_Item_Salvage_Quest_ScoutPatrol_Note",
              "status": "unassessed"
            },
            {
              "name": "Salvage Quest Secret Meeting Info",
              "technical": "DA_Item_Salvage_Quest_Secret_Meeting_Info",
              "status": "unassessed"
            },
            {
              "name": "Salvage Quest Seed Packet",
              "technical": "DA_Item_Salvage_Quest_SeedPacket",
              "status": "unassessed"
            },
            {
              "name": "Salvage Quest Seed Vault Key",
              "technical": "DA_Item_Salvage_Quest_SeedVault_Key",
              "status": "unassessed"
            },
            {
              "name": "Salvage Quest Terminal Key",
              "technical": "DA_Item_Salvage_Quest_TerminalKey",
              "status": "unassessed"
            },
            {
              "name": "Salvage Quest Video Tape",
              "technical": "DA_Item_Salvage_Quest_VideoTape",
              "status": "unassessed"
            },
            {
              "name": "Salvage Radio",
              "technical": "DA_Item_Salvage_Radio",
              "status": "unassessed"
            },
            {
              "name": "Salvage Radio Broken 01",
              "technical": "DA_Item_Salvage_RadioBroken_01",
              "status": "unassessed"
            },
            {
              "name": "Salvage Radio Relay",
              "technical": "DA_Item_Salvage_RadioRelay",
              "status": "unassessed"
            },
            {
              "name": "Salvage Raider Journal",
              "technical": "DA_Item_Salvage_RaiderJournal",
              "status": "unassessed"
            },
            {
              "name": "Salvage Raiders Refuge Security Code",
              "technical": "DA_Item_Salvage_RaidersRefugeSecurityCode",
              "status": "unassessed"
            },
            {
              "name": "Salvage Railway Lantern",
              "technical": "DA_Item_Salvage_RailwayLantern",
              "status": "unassessed"
            },
            {
              "name": "Salvage Rare Ascension Module",
              "technical": "DA_Item_Salvage_RareAscensionModule",
              "status": "unassessed"
            },
            {
              "name": "Salvage Red Coral Jewelry 01",
              "technical": "DA_Item_Salvage_RedCoralJewelry_01",
              "status": "unassessed"
            },
            {
              "name": "Salvage Refined Explosives",
              "technical": "DA_Item_Salvage_RefinedExplosives",
              "status": "unassessed"
            },
            {
              "name": "Salvage Remote Control",
              "technical": "DA_Item_Salvage_RemoteControl",
              "status": "unassessed"
            },
            {
              "name": "Salvage Riot Shield Broken 01",
              "technical": "DA_Item_Salvage_RiotShieldBroken_01",
              "status": "unassessed"
            },
            {
              "name": "Salvage Riven Tides Crane House Key",
              "technical": "DA_Item_Salvage_RivenTidesCraneHouseKey",
              "status": "unassessed"
            },
            {
              "name": "Salvage Riven Tides Hotel Key1",
              "technical": "DA_Item_Salvage_RivenTidesHotelKey1",
              "status": "unassessed"
            },
            {
              "name": "Salvage Riven Tides Hotel Key2",
              "technical": "DA_Item_Salvage_RivenTidesHotelKey2",
              "status": "unassessed"
            },
            {
              "name": "Salvage Riven Tides Hotel Key3",
              "technical": "DA_Item_Salvage_RivenTidesHotelKey3",
              "status": "unassessed"
            },
            {
              "name": "Salvage Riven Tides Hotel Key4",
              "technical": "DA_Item_Salvage_RivenTidesHotelKey4",
              "status": "unassessed"
            },
            {
              "name": "Salvage Riven Tides Hotel Key5",
              "technical": "DA_Item_Salvage_RivenTidesHotelKey5",
              "status": "unassessed"
            },
            {
              "name": "Salvage Riven Tides Hotel Key6",
              "technical": "DA_Item_Salvage_RivenTidesHotelKey6",
              "status": "unassessed"
            },
            {
              "name": "Salvage Riven Tides Hotel Key7",
              "technical": "DA_Item_Salvage_RivenTidesHotelKey7",
              "status": "unassessed"
            },
            {
              "name": "Salvage Riven Tides Wave Breaker Key1",
              "technical": "DA_Item_Salvage_RivenTidesWaveBreakerKey1",
              "status": "unassessed"
            },
            {
              "name": "Salvage Riven Tides Wave Breaker Key2",
              "technical": "DA_Item_Salvage_RivenTidesWaveBreakerKey2",
              "status": "unassessed"
            },
            {
              "name": "Salvage Rock Salt 01",
              "technical": "DA_Item_Salvage_RockSalt_01",
              "status": "unassessed"
            },
            {
              "name": "Salvage Rocket Thruster",
              "technical": "DA_Item_Salvage_RocketThruster",
              "status": "unassessed"
            },
            {
              "name": "Salvage Rollbot Part",
              "technical": "DA_Item_Salvage_Rollbot_Part",
              "status": "unassessed"
            },
            {
              "name": "Salvage Roots",
              "technical": "DA_Item_Salvage_Roots",
              "status": "unassessed"
            },
            {
              "name": "Salvage Rope",
              "technical": "DA_Item_Salvage_Rope",
              "status": "unassessed"
            },
            {
              "name": "Salvage Rosary 01",
              "technical": "DA_Item_Salvage_Rosary_01",
              "status": "unassessed"
            },
            {
              "name": "Salvage Rotary Encoder",
              "technical": "DA_Item_Salvage_RotaryEncoder",
              "status": "unassessed"
            },
            {
              "name": "Salvage Rubber",
              "technical": "DA_Item_Salvage_Rubber",
              "status": "unassessed"
            },
            {
              "name": "Salvage Rubber Band 01",
              "technical": "DA_Item_Salvage_RubberBand_01",
              "status": "unassessed"
            },
            {
              "name": "Salvage Rubber Pad",
              "technical": "DA_Item_Salvage_RubberPad",
              "status": "unassessed"
            },
            {
              "name": "Salvage Ruined Augment",
              "technical": "DA_Item_Salvage_RuinedAugment",
              "status": "unassessed"
            },
            {
              "name": "Salvage Runner Part",
              "technical": "DA_Item_Salvage_RunnerPart",
              "status": "unassessed"
            },
            {
              "name": "Salvage Rusted Bolts",
              "technical": "DA_Item_Salvage_RustedBolts",
              "status": "unassessed"
            },
            {
              "name": "Salvage Rusted Gear",
              "technical": "DA_Item_Salvage_RustedGear",
              "status": "unassessed"
            },
            {
              "name": "Salvage Rusted Tools",
              "technical": "DA_Item_Salvage_RustedTools",
              "status": "unassessed"
            },
            {
              "name": "Salvage Rusted Weapon",
              "technical": "DA_Item_Salvage_RustedWeapon",
              "status": "unassessed"
            },
            {
              "name": "Salvage Sample Cleaner",
              "technical": "DA_Item_Salvage_SampleCleaner",
              "status": "unassessed"
            },
            {
              "name": "Salvage Scanner 01",
              "technical": "DA_Item_Salvage_Scanner_01",
              "status": "unassessed"
            },
            {
              "name": "Salvage Scrap Metal",
              "technical": "DA_Item_Salvage_ScrapMetal",
              "status": "unassessed"
            },
            {
              "name": "Salvage Security Code",
              "technical": "DA_Item_Salvage_SecurityCode",
              "status": "unassessed"
            },
            {
              "name": "Salvage Seeds",
              "technical": "DA_Item_Salvage_Seeds",
              "status": "unassessed"
            },
            {
              "name": "Salvage Sensors",
              "technical": "DA_Item_Salvage_Sensors",
              "status": "unassessed"
            },
            {
              "name": "Salvage Sextant",
              "technical": "DA_Item_Salvage_Sextant",
              "status": "unassessed"
            },
            {
              "name": "Salvage Sheet Metal",
              "technical": "DA_Item_Salvage_SheetMetal",
              "status": "unassessed"
            },
            {
              "name": "Salvage Shotgun Parts",
              "technical": "DA_Item_Salvage_ShotgunParts",
              "status": "unassessed"
            },
            {
              "name": "Salvage Signal Amplifier",
              "technical": "DA_Item_Salvage_SignalAmplifier",
              "status": "unassessed"
            },
            {
              "name": "Salvage Silver Teaspoon Set 01",
              "technical": "DA_Item_Salvage_SilverTeaspoonSet_01",
              "status": "unassessed"
            },
            {
              "name": "Salvage Sniper Part",
              "technical": "DA_Item_Salvage_Sniper_Part",
              "status": "unassessed"
            },
            {
              "name": "Salvage Spaceport Utility Key1",
              "technical": "DA_Item_Salvage_SpaceportUtilityKey1",
              "status": "unassessed"
            },
            {
              "name": "Salvage Spaceport Utility Key2",
              "technical": "DA_Item_Salvage_SpaceportUtilityKey2",
              "status": "unassessed"
            },
            {
              "name": "Salvage Spaceport Utility Key3",
              "technical": "DA_Item_Salvage_SpaceportUtilityKey3",
              "status": "unassessed"
            },
            {
              "name": "Salvage Spaceport Utility Key4",
              "technical": "DA_Item_Salvage_SpaceportUtilityKey4",
              "status": "unassessed"
            },
            {
              "name": "Salvage Speaker Component",
              "technical": "DA_Item_Salvage_SpeakerComponent",
              "status": "unassessed"
            },
            {
              "name": "Salvage Spearmint Part",
              "technical": "DA_Item_Salvage_SpearmintPart",
              "status": "unassessed"
            },
            {
              "name": "Salvage Spectrometer",
              "technical": "DA_Item_Salvage_Spectrometer",
              "status": "unassessed"
            },
            {
              "name": "Salvage Spectrum Analyzer",
              "technical": "DA_Item_Salvage_SpectrumAnalyzer",
              "status": "unassessed"
            },
            {
              "name": "Salvage Spring Cushion",
              "technical": "DA_Item_Salvage_SpringCushion",
              "status": "unassessed"
            },
            {
              "name": "Salvage Statuette 01",
              "technical": "DA_Item_Salvage_Statuette_01",
              "status": "unassessed"
            },
            {
              "name": "Salvage Steel Spring",
              "technical": "DA_Item_Salvage_SteelSpring",
              "status": "unassessed"
            },
            {
              "name": "Salvage Synthesized Fuel",
              "technical": "DA_Item_Salvage_SynthesizedFuel",
              "status": "unassessed"
            },
            {
              "name": "Salvage Syringe",
              "technical": "DA_Item_Salvage_Syringe",
              "status": "unassessed"
            },
            {
              "name": "Salvage Tactical West",
              "technical": "DA_Item_Salvage_TacticalWest",
              "status": "unassessed"
            },
            {
              "name": "Salvage Tarragon Part",
              "technical": "DA_Item_Salvage_Tarragon_Part",
              "status": "unassessed"
            },
            {
              "name": "Salvage Taser Broken 01",
              "technical": "DA_Item_Salvage_TaserBroken_01",
              "status": "unassessed"
            },
            {
              "name": "Salvage Tattered Clothes",
              "technical": "DA_Item_Salvage_TatteredClothes",
              "status": "unassessed"
            },
            {
              "name": "Salvage Telemetry Transceiver",
              "technical": "DA_Item_Salvage_TelemetryTransceiver",
              "status": "unassessed"
            },
            {
              "name": "Salvage Tellurion 01",
              "technical": "DA_Item_Salvage_Tellurion_01",
              "status": "unassessed"
            },
            {
              "name": "Salvage The Dam Utility Key1",
              "technical": "DA_Item_Salvage_TheDamUtilityKey1",
              "status": "unassessed"
            },
            {
              "name": "Salvage The Dam Utility Key2",
              "technical": "DA_Item_Salvage_TheDamUtilityKey2",
              "status": "unassessed"
            },
            {
              "name": "Salvage The Dam Utility Key3",
              "technical": "DA_Item_Salvage_TheDamUtilityKey3",
              "status": "unassessed"
            },
            {
              "name": "Salvage The Dam Utility Key4",
              "technical": "DA_Item_Salvage_TheDamUtilityKey4",
              "status": "unassessed"
            },
            {
              "name": "Salvage Thermostat",
              "technical": "DA_Item_Salvage_Thermostat",
              "status": "unassessed"
            },
            {
              "name": "Salvage Toaster",
              "technical": "DA_Item_Salvage_Toaster",
              "status": "unassessed"
            },
            {
              "name": "Salvage Torch Ginger",
              "technical": "DA_Item_Salvage_TorchGinger",
              "status": "unassessed"
            },
            {
              "name": "Salvage Torn Blanket",
              "technical": "DA_Item_Salvage_TornBlanket",
              "status": "unassessed"
            },
            {
              "name": "Salvage Train Model",
              "technical": "DA_Item_Salvage_TrainModel",
              "status": "unassessed"
            },
            {
              "name": "Salvage Trinket Air Freshener",
              "technical": "DA_Item_Salvage_Trinket_AirFreshener",
              "status": "unassessed"
            },
            {
              "name": "Salvage Trinket Bent Clothes Hanger",
              "technical": "DA_Item_Salvage_Trinket_BentClothesHanger",
              "status": "unassessed"
            },
            {
              "name": "Salvage Trinket Blown Fuses",
              "technical": "DA_Item_Salvage_Trinket_BlownFuses",
              "status": "unassessed"
            },
            {
              "name": "Salvage Trinket Boat Model A",
              "technical": "DA_Item_Salvage_Trinket_BoatModel_A",
              "status": "unassessed"
            },
            {
              "name": "Salvage Trinket Boat Model A 02",
              "technical": "DA_Item_Salvage_Trinket_BoatModel_A_02",
              "status": "unassessed"
            },
            {
              "name": "Salvage Trinket Boat Model B",
              "technical": "DA_Item_Salvage_Trinket_BoatModel_B",
              "status": "unassessed"
            },
            {
              "name": "Salvage Trinket Boat Model B 02",
              "technical": "DA_Item_Salvage_Trinket_BoatModel_B_02",
              "status": "unassessed"
            },
            {
              "name": "Salvage Trinket Boat Model C",
              "technical": "DA_Item_Salvage_Trinket_BoatModel_C",
              "status": "unassessed"
            },
            {
              "name": "Salvage Trinket Boat Model C 02",
              "technical": "DA_Item_Salvage_Trinket_BoatModel_C_02",
              "status": "unassessed"
            },
            {
              "name": "Salvage Trinket Boat Model D",
              "technical": "DA_Item_Salvage_Trinket_BoatModel_D",
              "status": "unassessed"
            },
            {
              "name": "Salvage Trinket Boat Model D 02",
              "technical": "DA_Item_Salvage_Trinket_BoatModel_D_02",
              "status": "unassessed"
            },
            {
              "name": "Salvage Trinket Boat Model E",
              "technical": "DA_Item_Salvage_Trinket_BoatModel_E",
              "status": "unassessed"
            },
            {
              "name": "Salvage Trinket Boat Model E 02",
              "technical": "DA_Item_Salvage_Trinket_BoatModel_E_02",
              "status": "unassessed"
            },
            {
              "name": "Salvage Trinket Breathtaking Snow Globe",
              "technical": "DA_Item_Salvage_Trinket_BreathtakingSnowGlobe",
              "status": "unassessed"
            },
            {
              "name": "Salvage Trinket Burntout Candles",
              "technical": "DA_Item_Salvage_Trinket_BurntoutCandles",
              "status": "unassessed"
            },
            {
              "name": "Salvage Trinket Coffee Pot",
              "technical": "DA_Item_Salvage_Trinket_CoffeePot",
              "status": "unassessed"
            },
            {
              "name": "Salvage Trinket Deflated Football",
              "technical": "DA_Item_Salvage_Trinket_DeflatedFootball",
              "status": "unassessed"
            },
            {
              "name": "Salvage Trinket Dog Collar",
              "technical": "DA_Item_Salvage_Trinket_DogCollar",
              "status": "unassessed"
            },
            {
              "name": "Salvage Trinket Empty Wine Bottle",
              "technical": "DA_Item_Salvage_Trinket_EmptyWineBottle",
              "status": "unassessed"
            },
            {
              "name": "Salvage Trinket Expired Respirator",
              "technical": "DA_Item_Salvage_Trinket_ExpiredRespirator",
              "status": "unassessed"
            },
            {
              "name": "Salvage Trinket Faded Picture Frame",
              "technical": "DA_Item_Salvage_Trinket_FadedPictureFrame",
              "status": "unassessed"
            },
            {
              "name": "Salvage Trinket Food Wrapper",
              "technical": "DA_Item_Salvage_Trinket_FoodWrapper",
              "status": "unassessed"
            },
            {
              "name": "Salvage Trinket Garlic Press",
              "technical": "DA_Item_Salvage_Trinket_GarlicPress",
              "status": "unassessed"
            },
            {
              "name": "Salvage Trinket Husk Lily",
              "technical": "DA_Item_Salvage_Trinket_HuskLily",
              "status": "unassessed"
            },
            {
              "name": "Salvage Trinket Ice Cream Scooper",
              "technical": "DA_Item_Salvage_Trinket_IceCreamScooper",
              "status": "unassessed"
            },
            {
              "name": "Salvage Trinket Lance Mix Tape",
              "technical": "DA_Item_Salvage_Trinket_LanceMixTape",
              "status": "unassessed"
            },
            {
              "name": "Salvage Trinket Light Bulb",
              "technical": "DA_Item_Salvage_Trinket_LightBulb",
              "status": "unassessed"
            },
            {
              "name": "Salvage Trinket Playing Cards",
              "technical": "DA_Item_Salvage_Trinket_PlayingCards",
              "status": "unassessed"
            },
            {
              "name": "Salvage Trinket Ripped Safety Vest",
              "technical": "DA_Item_Salvage_Trinket_RippedSafetyVest",
              "status": "unassessed"
            },
            {
              "name": "Salvage Trinket Torn Book",
              "technical": "DA_Item_Salvage_Trinket_TornBook",
              "status": "unassessed"
            },
            {
              "name": "Salvage Turbo Pump 01",
              "technical": "DA_Item_Salvage_TurboPump_01",
              "status": "unassessed"
            },
            {
              "name": "Salvage Uncommon Ascension Module",
              "technical": "DA_Item_Salvage_UncommonAscensionModule",
              "status": "unassessed"
            },
            {
              "name": "Salvage Vase 01",
              "technical": "DA_Item_Salvage_Vase_01",
              "status": "unassessed"
            },
            {
              "name": "Salvage Vintage Steering Wheel 01",
              "technical": "DA_Item_Salvage_VintageSteeringWheel_01",
              "status": "unassessed"
            },
            {
              "name": "Salvage Voltage Converter",
              "technical": "DA_Item_Salvage_VoltageConverter",
              "status": "unassessed"
            },
            {
              "name": "Salvage Water Filter",
              "technical": "DA_Item_Salvage_WaterFilter",
              "status": "unassessed"
            },
            {
              "name": "Salvage Water Pump",
              "technical": "DA_Item_Salvage_WaterPump",
              "status": "unassessed"
            },
            {
              "name": "Salvage Wires",
              "technical": "DA_Item_Salvage_Wires",
              "status": "unassessed"
            },
            {
              "name": "Salvage Woven Cloth",
              "technical": "DA_Item_Salvage_WovenCloth",
              "status": "unassessed"
            }
          ]
        },
        {
          "name": "Firearms",
          "children": [
            {
              "name": "Assault Rifle Bullpup 01",
              "technical": "DA_Item_AssaultRifle_Bullpup_01",
              "status": "unassessed"
            },
            {
              "name": "Assault Rifle Burst 01",
              "technical": "DA_Item_AssaultRifle_Burst_01",
              "status": "unassessed"
            },
            {
              "name": "Assault Rifle Heavy 01",
              "technical": "DA_Item_AssaultRifle_Heavy_01",
              "status": "unassessed"
            },
            {
              "name": "Assault Rifle Heavy 01 Ascended",
              "technical": "DA_Item_AssaultRifle_Heavy_01_Ascended",
              "status": "unassessed"
            },
            {
              "name": "Assault Rifle Heavy 01 Bigger Mag",
              "technical": "DA_Item_AssaultRifle_Heavy_01_BiggerMag",
              "status": "unassessed"
            },
            {
              "name": "Assault Rifle Heavy 01 Semi Auto",
              "technical": "DA_Item_AssaultRifle_Heavy_01_SemiAuto",
              "status": "unassessed"
            },
            {
              "name": "Assault Rifle Heavy 01 Semi Auto XRounds",
              "technical": "DA_Item_AssaultRifle_Heavy_01_SemiAuto_XRounds",
              "status": "unassessed"
            },
            {
              "name": "Assault Rifle Low Tier 01",
              "technical": "DA_Item_AssaultRifle_LowTier_01",
              "status": "unassessed"
            },
            {
              "name": "Assault Rifle Low Tier 01 Ascended",
              "technical": "DA_Item_AssaultRifle_LowTier_01_Ascended",
              "status": "unassessed"
            },
            {
              "name": "Assault Rifle Low Tier 01 Incendiary Rounds",
              "technical": "DA_Item_AssaultRifle_LowTier_01_IncendiaryRounds",
              "status": "unassessed"
            },
            {
              "name": "Assault Rifle Low Tier 01 More Reload",
              "technical": "DA_Item_AssaultRifle_LowTier_01_MoreReload",
              "status": "unassessed"
            },
            {
              "name": "Assault Rifle Low Tier 01 More Reload Drum Mag",
              "technical": "DA_Item_AssaultRifle_LowTier_01_MoreReload_DrumMag",
              "status": "unassessed"
            },
            {
              "name": "Assault Rifle Low Tier 01 More Reload Drum Mag Incendiary Rounds",
              "technical": "DA_Item_AssaultRifle_LowTier_01_MoreReload_DrumMag_IncendiaryRounds",
              "status": "unassessed"
            },
            {
              "name": "Assault Rifle Low Tier 01 More Reload Incendiary Rounds",
              "technical": "DA_Item_AssaultRifle_LowTier_01_MoreReload_IncendiaryRounds",
              "status": "unassessed"
            },
            {
              "name": "Assault Rifle Pneumatic 01",
              "technical": "DA_Item_AssaultRifle_Pneumatic_01",
              "status": "unassessed"
            },
            {
              "name": "Assault Rifle Pneumatic 01 Anti Shield",
              "technical": "DA_Item_AssaultRifle_Pneumatic_01_AntiShield",
              "status": "unassessed"
            },
            {
              "name": "Assault Rifle Pneumatic 01 Anti Shield Full Auto",
              "technical": "DA_Item_AssaultRifle_Pneumatic_01_AntiShield_FullAuto",
              "status": "unassessed"
            },
            {
              "name": "Assault Rifle Pneumatic 01 Ascended",
              "technical": "DA_Item_AssaultRifle_Pneumatic_01_Ascended",
              "status": "unassessed"
            },
            {
              "name": "Assault Rifle Pneumatic 01 Full Auto",
              "technical": "DA_Item_AssaultRifle_Pneumatic_01_FullAuto",
              "status": "unassessed"
            },
            {
              "name": "Assault Rifle Pneumatic 01 XRounds",
              "technical": "DA_Item_AssaultRifle_Pneumatic_01_XRounds",
              "status": "unassessed"
            },
            {
              "name": "Ferro I",
              "technical": "DA_Item_BattleRifle_BreachAction_01",
              "status": "partial",
              "note": "Menu equipment, empty muzzle, underbarrel and stock mod slots in the menu and Raid, Raid inventory and weight, drawing and switching, fire cadence and single-round reloads from carried Heavy Ammo are stock-client verified, with known issues. Dropping it from the Raid inventory and picking it up again is stock-client verified; dropping the weapon in hand is not handled. Damage, ballistics, fitting mods and upgrades remain."
            },
            {
              "name": "Battle Rifle Energy Burst 01",
              "technical": "DA_Item_BattleRifle_EnergyBurst_01",
              "status": "unassessed"
            },
            {
              "name": "Battle Rifle Energy Burst 01 Ascended",
              "technical": "DA_Item_BattleRifle_EnergyBurst_01_Ascended",
              "status": "unassessed"
            },
            {
              "name": "Battle Rifle Energy Burst 01 Bigger Mag",
              "technical": "DA_Item_BattleRifle_EnergyBurst_01_BiggerMag",
              "status": "unassessed"
            },
            {
              "name": "Battle Rifle Energy Burst 01 Bigger Mag Charged Burst",
              "technical": "DA_Item_BattleRifle_EnergyBurst_01_BiggerMag_ChargedBurst",
              "status": "unassessed"
            },
            {
              "name": "Battle Rifle Energy Burst 01 Bigger Mag Charged Burst Incendiary Rounds",
              "technical": "DA_Item_BattleRifle_EnergyBurst_01_BiggerMag_ChargedBurst_IncendiaryRounds",
              "status": "unassessed"
            },
            {
              "name": "Battle Rifle Energy Burst 01 Bigger Mag Charged Burst Increased Burst",
              "technical": "DA_Item_BattleRifle_EnergyBurst_01_BiggerMag_ChargedBurst_IncreasedBurst",
              "status": "unassessed"
            },
            {
              "name": "Battle Rifle Energy Burst 01 Bigger Mag Charged Burst Increased Burst Incendiary Rounds",
              "technical": "DA_Item_BattleRifle_EnergyBurst_01_BiggerMag_ChargedBurst_IncreasedBurst_IncendiaryRounds",
              "status": "unassessed"
            },
            {
              "name": "Battle Rifle Energy Burst 01 Bigger Mag Incendiary Rounds",
              "technical": "DA_Item_BattleRifle_EnergyBurst_01_BiggerMag_IncendiaryRounds",
              "status": "unassessed"
            },
            {
              "name": "Battle Rifle Energy Burst 01 Charged Burst",
              "technical": "DA_Item_BattleRifle_EnergyBurst_01_ChargedBurst",
              "status": "unassessed"
            },
            {
              "name": "Battle Rifle Energy Burst 01 Charged Burst Incendiary Rounds",
              "technical": "DA_Item_BattleRifle_EnergyBurst_01_ChargedBurst_IncendiaryRounds",
              "status": "unassessed"
            },
            {
              "name": "Battle Rifle Energy Burst 01 Charged Burst Increased Burst",
              "technical": "DA_Item_BattleRifle_EnergyBurst_01_ChargedBurst_IncreasedBurst",
              "status": "unassessed"
            },
            {
              "name": "Battle Rifle Energy Burst 01 Charged Burst Increased Burst Incendiary Rounds",
              "technical": "DA_Item_BattleRifle_EnergyBurst_01_ChargedBurst_IncreasedBurst_IncendiaryRounds",
              "status": "unassessed"
            },
            {
              "name": "Battle Rifle Energy Burst 01 Incendiary Rounds",
              "technical": "DA_Item_BattleRifle_EnergyBurst_01_IncendiaryRounds",
              "status": "unassessed"
            },
            {
              "name": "Battle Rifle Lever Action 01",
              "technical": "DA_Item_BattleRifle_LeverAction_01",
              "status": "unassessed"
            },
            {
              "name": "Battle Rifle Lever Action 01 Ascended",
              "technical": "DA_Item_BattleRifle_LeverAction_01_Ascended",
              "status": "unassessed"
            },
            {
              "name": "Battle Rifle Lever Action 01 More Reload",
              "technical": "DA_Item_BattleRifle_LeverAction_01_MoreReload",
              "status": "unassessed"
            },
            {
              "name": "Battle Rifle Lever Action 01 More Reload Scoped",
              "technical": "DA_Item_BattleRifle_LeverAction_01_MoreReload_Scoped",
              "status": "unassessed"
            },
            {
              "name": "Battle Rifle Lever Action 01 Scoped",
              "technical": "DA_Item_BattleRifle_LeverAction_01_Scoped",
              "status": "unassessed"
            },
            {
              "name": "Battle Rifle Semi Auto Light 01",
              "technical": "DA_Item_BattleRifle_SemiAuto_Light_01",
              "status": "unassessed"
            },
            {
              "name": "Hand Cannon Double Action Snubnose 01",
              "technical": "DA_Item_HandCannon_DoubleAction_Snubnose_01",
              "status": "unassessed"
            },
            {
              "name": "Hand Cannon Single Action 01",
              "technical": "DA_Item_HandCannon_SingleAction_01",
              "status": "unassessed"
            },
            {
              "name": "Hand Cannon Single Action 01 Ascended",
              "technical": "DA_Item_HandCannon_SingleAction_01_Ascended",
              "status": "unassessed"
            },
            {
              "name": "Hand Cannon Single Action 01 Projectile Splitter",
              "technical": "DA_Item_HandCannon_SingleAction_01_ProjectileSplitter",
              "status": "unassessed"
            },
            {
              "name": "Hand Cannon Single Action 01 XRounds",
              "technical": "DA_Item_HandCannon_SingleAction_01_XRounds",
              "status": "unassessed"
            },
            {
              "name": "LMG Medium 01",
              "technical": "DA_Item_LMG_Medium_01",
              "status": "unassessed"
            },
            {
              "name": "Launcher Anti Arc Medium 01",
              "technical": "DA_Item_Launcher_AntiArc_Medium_01",
              "status": "unassessed"
            },
            {
              "name": "Launcher Anti Arc Medium 01 Ascended",
              "technical": "DA_Item_Launcher_AntiArc_Medium_01_Ascended",
              "status": "unassessed"
            },
            {
              "name": "Launcher Anti Arc Medium 01 Incendiary Rounds",
              "technical": "DA_Item_Launcher_AntiArc_Medium_01_IncendiaryRounds",
              "status": "unassessed"
            },
            {
              "name": "Launcher Anti Arc Medium 01 Incendiary Rounds Mag Reload",
              "technical": "DA_Item_Launcher_AntiArc_Medium_01_IncendiaryRounds_MagReload",
              "status": "unassessed"
            },
            {
              "name": "Launcher Anti Arc Medium 01 Mag Reload",
              "technical": "DA_Item_Launcher_AntiArc_Medium_01_MagReload",
              "status": "unassessed"
            },
            {
              "name": "Launcher Anti Arc Single Shot 01",
              "technical": "DA_Item_Launcher_AntiArc_SingleShot_01",
              "status": "unassessed"
            },
            {
              "name": "Launcher Anti Arc Single Shot 01 Ascended",
              "technical": "DA_Item_Launcher_AntiArc_SingleShot_01_Ascended",
              "status": "unassessed"
            },
            {
              "name": "Launcher Anti Arc Single Shot 01 High Velocity",
              "technical": "DA_Item_Launcher_AntiArc_SingleShot_01_HighVelocity",
              "status": "unassessed"
            },
            {
              "name": "Launcher Anti Arc Single Shot 01 High Velocity Incendiary Rounds",
              "technical": "DA_Item_Launcher_AntiArc_SingleShot_01_HighVelocity_IncendiaryRounds",
              "status": "unassessed"
            },
            {
              "name": "Launcher Anti Arc Single Shot 01 High Velocity Sprint Shooting",
              "technical": "DA_Item_Launcher_AntiArc_SingleShot_01_HighVelocity_SprintShooting",
              "status": "unassessed"
            },
            {
              "name": "Launcher Anti Arc Single Shot 01 High Velocity Sprint Shooting Incendiary Rounds",
              "technical": "DA_Item_Launcher_AntiArc_SingleShot_01_HighVelocity_SprintShooting_IncendiaryRounds",
              "status": "unassessed"
            },
            {
              "name": "Launcher Anti Arc Single Shot 01 Incendiary Rounds",
              "technical": "DA_Item_Launcher_AntiArc_SingleShot_01_IncendiaryRounds",
              "status": "unassessed"
            },
            {
              "name": "Launcher Anti Arc Single Shot 01 Sprint Shooting",
              "technical": "DA_Item_Launcher_AntiArc_SingleShot_01_SprintShooting",
              "status": "unassessed"
            },
            {
              "name": "Launcher Anti Arc Single Shot 01 Sprint Shooting Incendiary Rounds",
              "technical": "DA_Item_Launcher_AntiArc_SingleShot_01_SprintShooting_IncendiaryRounds",
              "status": "unassessed"
            },
            {
              "name": "Pistol 01",
              "technical": "DA_Item_Pistol_01",
              "status": "unassessed"
            },
            {
              "name": "Pistol 01 Ascended",
              "technical": "DA_Item_Pistol_01_Ascended",
              "status": "unassessed"
            },
            {
              "name": "Pistol 01 Burst",
              "technical": "DA_Item_Pistol_01_Burst",
              "status": "unassessed"
            },
            {
              "name": "Pistol 01 Burst Increased Burst",
              "technical": "DA_Item_Pistol_01_Burst_IncreasedBurst",
              "status": "unassessed"
            },
            {
              "name": "Pistol 01 Burst Increased Burst Sprint Shooting",
              "technical": "DA_Item_Pistol_01_Burst_IncreasedBurst_SprintShooting",
              "status": "unassessed"
            },
            {
              "name": "Pistol 01 Burst Sprint Shooting",
              "technical": "DA_Item_Pistol_01_Burst_SprintShooting",
              "status": "unassessed"
            },
            {
              "name": "Pistol 01 Full Auto",
              "technical": "DA_Item_Pistol_01_FullAuto",
              "status": "unassessed"
            },
            {
              "name": "Pistol 01 Full Auto Sprint Shooting",
              "technical": "DA_Item_Pistol_01_FullAuto_SprintShooting",
              "status": "unassessed"
            },
            {
              "name": "Pistol 01 Sprint Shooting",
              "technical": "DA_Item_Pistol_01_SprintShooting",
              "status": "unassessed"
            },
            {
              "name": "Pistol High Power 01",
              "technical": "DA_Item_Pistol_HighPower_01",
              "status": "unassessed"
            },
            {
              "name": "Pistol Silenced 01",
              "technical": "DA_Item_Pistol_Silenced_01",
              "status": "unassessed"
            },
            {
              "name": "Pistol Silenced 01 Ascended",
              "technical": "DA_Item_Pistol_Silenced_01_Ascended",
              "status": "unassessed"
            },
            {
              "name": "Pistol Silenced 01 Incendiary Rounds",
              "technical": "DA_Item_Pistol_Silenced_01_IncendiaryRounds",
              "status": "unassessed"
            },
            {
              "name": "Pistol Silenced 01 Semi Auto",
              "technical": "DA_Item_Pistol_Silenced_01_SemiAuto",
              "status": "unassessed"
            },
            {
              "name": "Pistol Silenced 01 Semi Auto Sprint Shooting",
              "technical": "DA_Item_Pistol_Silenced_01_SemiAuto_SprintShooting",
              "status": "unassessed"
            },
            {
              "name": "Pistol Silenced 01 Tracker Rounds",
              "technical": "DA_Item_Pistol_Silenced_01_TrackerRounds",
              "status": "unassessed"
            },
            {
              "name": "SMG 01",
              "technical": "DA_Item_SMG_01",
              "status": "unassessed"
            },
            {
              "name": "SMG Low Tier 01",
              "technical": "DA_Item_SMG_LowTier_01",
              "status": "unassessed"
            },
            {
              "name": "SMG Medium 01",
              "technical": "DA_Item_SMG_Medium_01",
              "status": "unassessed"
            },
            {
              "name": "SMG Medium 01 Ascended",
              "technical": "DA_Item_SMG_Medium_01_Ascended",
              "status": "unassessed"
            },
            {
              "name": "SMG Medium 01 Carbine",
              "technical": "DA_Item_SMG_Medium_01_Carbine",
              "status": "unassessed"
            },
            {
              "name": "SMG Medium 01 Incendiary Rounds",
              "technical": "DA_Item_SMG_Medium_01_IncendiaryRounds",
              "status": "unassessed"
            },
            {
              "name": "SMG Medium 01 Incendiary Rounds Carbine",
              "technical": "DA_Item_SMG_Medium_01_IncendiaryRounds_Carbine",
              "status": "unassessed"
            },
            {
              "name": "SMG Medium 01 Incendiary Rounds Sprint Shooting",
              "technical": "DA_Item_SMG_Medium_01_IncendiaryRounds_SprintShooting",
              "status": "unassessed"
            },
            {
              "name": "SMG Medium 01 Sprint Shooting",
              "technical": "DA_Item_SMG_Medium_01_SprintShooting",
              "status": "unassessed"
            },
            {
              "name": "Shotgun Energy Adjustable 01",
              "technical": "DA_Item_Shotgun_Energy_Adjustable_01",
              "status": "unassessed"
            },
            {
              "name": "Shotgun Pump Action 01",
              "technical": "DA_Item_Shotgun_PumpAction_01",
              "status": "unassessed"
            },
            {
              "name": "Shotgun Pump Action 01 Ascended",
              "technical": "DA_Item_Shotgun_PumpAction_01_Ascended",
              "status": "unassessed"
            },
            {
              "name": "Shotgun Pump Action 01 More Reload",
              "technical": "DA_Item_Shotgun_PumpAction_01_MoreReload",
              "status": "unassessed"
            },
            {
              "name": "Shotgun Pump Action 01 More Reload Slug Rounds",
              "technical": "DA_Item_Shotgun_PumpAction_01_MoreReload_SlugRounds",
              "status": "unassessed"
            },
            {
              "name": "Shotgun Pump Action 01 Slug Rounds",
              "technical": "DA_Item_Shotgun_PumpAction_01_SlugRounds",
              "status": "unassessed"
            },
            {
              "name": "Shotgun Semi Auto 01",
              "technical": "DA_Item_Shotgun_SemiAuto_01",
              "status": "unassessed"
            },
            {
              "name": "Sniper Rifle Bolt Action Medium 01",
              "technical": "DA_Item_SniperRifle_BoltAction_Medium_01",
              "status": "unassessed"
            },
            {
              "name": "Sniper Rifle Bolt Action Medium 01 Anti Weak Point",
              "technical": "DA_Item_SniperRifle_BoltAction_Medium_01_AntiWeakPoint",
              "status": "unassessed"
            },
            {
              "name": "Sniper Rifle Bolt Action Medium 01 Ascended",
              "technical": "DA_Item_SniperRifle_BoltAction_Medium_01_Ascended",
              "status": "unassessed"
            },
            {
              "name": "Sniper Rifle Bolt Action Medium 01 Straight Bolt",
              "technical": "DA_Item_SniperRifle_BoltAction_Medium_01_StraightBolt",
              "status": "unassessed"
            },
            {
              "name": "Sniper Rifle Bolt Action Medium 01 Straight Bolt Anti Weak Point",
              "technical": "DA_Item_SniperRifle_BoltAction_Medium_01_StraightBolt_AntiWeakPoint",
              "status": "unassessed"
            },
            {
              "name": "Sniper Rifle Bolt Action Medium 01 Tracker Rounds",
              "technical": "DA_Item_SniperRifle_BoltAction_Medium_01_TrackerRounds",
              "status": "unassessed"
            },
            {
              "name": "Sniper Rifle Bolt Action Medium 01 Tracker Rounds Straight Bolt",
              "technical": "DA_Item_SniperRifle_BoltAction_Medium_01_TrackerRounds_StraightBolt",
              "status": "unassessed"
            }
          ]
        },
        {
          "name": "Ammunition",
          "children": [
            {
              "name": "Energy Ammo",
              "technical": "DA_Item_EnergyAmmo",
              "status": "unassessed"
            },
            {
              "name": "Heavy Ammo",
              "technical": "DA_Item_HeavyAmmo",
              "status": "partial",
              "note": "Stacks of up to 60 rounds, menu and Raid inventory, weight and whole-stack moves are supported, and menu stacks can be split and merged; Ferro I reloads take rounds from carried stacks. Dropping stacks in Raid and picking them up again is stock-client verified. Splitting and merging in Raid remain."
            },
            {
              "name": "Launcher Ammo",
              "technical": "DA_Item_LauncherAmmo",
              "status": "unassessed"
            },
            {
              "name": "Light Ammo",
              "technical": "DA_Item_LightAmmo",
              "status": "unassessed"
            },
            {
              "name": "Medium Ammo",
              "technical": "DA_Item_MediumAmmo",
              "status": "unassessed"
            },
            {
              "name": "Shotgun Ammo",
              "technical": "DA_Item_ShotgunAmmo",
              "status": "unassessed"
            }
          ]
        },
        {
          "name": "Furniture blueprints",
          "children": [
            {
              "name": "Outpost Furniture Recipe Alpine Wardrobe 01",
              "technical": "DA_Item_OutpostFurnitureRecipe_AlpineWardrobe_01",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Bar Stool 01 A",
              "technical": "DA_Item_OutpostFurnitureRecipe_BarStool_01_A",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Bar Table 01 A",
              "technical": "DA_Item_OutpostFurnitureRecipe_BarTable_01_A",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Bird Cage Project Table 01 A",
              "technical": "DA_Item_OutpostFurnitureRecipe_BirdCageProject_Table_01_A",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Bird Cage Project Table 02 A",
              "technical": "DA_Item_OutpostFurnitureRecipe_BirdCageProject_Table_02_A",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe CA Lamp Floor 02 A",
              "technical": "DA_Item_OutpostFurnitureRecipe_CA_LampFloor_02_A",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Cafeteria Table 01 A",
              "technical": "DA_Item_OutpostFurnitureRecipe_CafeteriaTable_01_A",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Chambers Bed Table 01 A",
              "technical": "DA_Item_OutpostFurnitureRecipe_Chambers_BedTable_01_A",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Chambers Lamp 01 A",
              "technical": "DA_Item_OutpostFurnitureRecipe_Chambers_Lamp_01_A",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Chambers Shelf 01 A",
              "technical": "DA_Item_OutpostFurnitureRecipe_Chambers_Shelf_01_A",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Chandelier 01 A",
              "technical": "DA_Item_OutpostFurnitureRecipe_Chandelier_01_A",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Cmr Chair 01 A",
              "technical": "DA_Item_OutpostFurnitureRecipe_Cmr_Chair_01_A",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Cmr Chair 03 A",
              "technical": "DA_Item_OutpostFurnitureRecipe_Cmr_Chair_03_A",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Cmr Cupboard Cafe 01 A",
              "technical": "DA_Item_OutpostFurnitureRecipe_Cmr_CupboardCafe_01_A",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Cmr Lamp Ceiling 01 A",
              "technical": "DA_Item_OutpostFurnitureRecipe_Cmr_LampCeiling_01_A",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Cmr Shelf 03 A",
              "technical": "DA_Item_OutpostFurnitureRecipe_Cmr_Shelf_03_A",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Cmr Sofa 03 A",
              "technical": "DA_Item_OutpostFurnitureRecipe_Cmr_Sofa_03_A",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Cmr Table 02 A",
              "technical": "DA_Item_OutpostFurnitureRecipe_Cmr_Table_02_A",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Console Chest 01 A",
              "technical": "DA_Item_OutpostFurnitureRecipe_ConsoleChest_01_A",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Construction Light 01 A",
              "technical": "DA_Item_OutpostFurnitureRecipe_ConstructionLight_01_A",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Debris Draped 01",
              "technical": "DA_Item_OutpostFurnitureRecipe_DebrisDraped_01",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Debris Draped 02",
              "technical": "DA_Item_OutpostFurnitureRecipe_DebrisDraped_02",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Debris Draped 03",
              "technical": "DA_Item_OutpostFurnitureRecipe_DebrisDraped_03",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Debris Draped 04",
              "technical": "DA_Item_OutpostFurnitureRecipe_DebrisDraped_04",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Debris Draped 05",
              "technical": "DA_Item_OutpostFurnitureRecipe_DebrisDraped_05",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Debris Draped 06",
              "technical": "DA_Item_OutpostFurnitureRecipe_DebrisDraped_06",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Floor Lamp 02 A",
              "technical": "DA_Item_OutpostFurnitureRecipe_FloorLamp_02_A",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Glass Ceiling Lamp 01 A",
              "technical": "DA_Item_OutpostFurnitureRecipe_GlassCeilingLamp_01_A",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Gvt Chair 01",
              "technical": "DA_Item_OutpostFurnitureRecipe_Gvt_Chair_01",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Gvt Chair 02 A",
              "technical": "DA_Item_OutpostFurnitureRecipe_Gvt_Chair_02_A",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Gvt Lamp Ceiling 01 A",
              "technical": "DA_Item_OutpostFurnitureRecipe_Gvt_LampCeiling_01_A",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Gvt Sofa 01 A",
              "technical": "DA_Item_OutpostFurnitureRecipe_Gvt_Sofa_01_A",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Gvt Sofa 02 A",
              "technical": "DA_Item_OutpostFurnitureRecipe_Gvt_Sofa_02_A",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Gvt Sofa 03 A",
              "technical": "DA_Item_OutpostFurnitureRecipe_Gvt_Sofa_03_A",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe HOR Chair 01 A",
              "technical": "DA_Item_OutpostFurnitureRecipe_HOR_Chair_01_A",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Hme Stool 01 A",
              "technical": "DA_Item_OutpostFurnitureRecipe_Hme_Stool_01_A",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Offerings Console",
              "technical": "DA_Item_OutpostFurnitureRecipe_OfferingsConsole",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Project Winter Table 01 A",
              "technical": "DA_Item_OutpostFurnitureRecipe_ProjectWinter_Table_01_A",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe RST Table 01 A",
              "technical": "DA_Item_OutpostFurnitureRecipe_RST_Table_01_A",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Raider Den Bed 01",
              "technical": "DA_Item_OutpostFurnitureRecipe_RaiderDen_Bed_01",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Raider Den Bed 02",
              "technical": "DA_Item_OutpostFurnitureRecipe_RaiderDen_Bed_02",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Raider Den Bed 03",
              "technical": "DA_Item_OutpostFurnitureRecipe_RaiderDen_Bed_03",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Raider Den Bed 04",
              "technical": "DA_Item_OutpostFurnitureRecipe_RaiderDen_Bed_04",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Raider Den Shelves 01",
              "technical": "DA_Item_OutpostFurnitureRecipe_RaiderDen_Shelves_01",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Raider Den Shelves 02",
              "technical": "DA_Item_OutpostFurnitureRecipe_RaiderDen_Shelves_02",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Raider Den Shelves 03",
              "technical": "DA_Item_OutpostFurnitureRecipe_RaiderDen_Shelves_03",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Raider Den Shelves 04",
              "technical": "DA_Item_OutpostFurnitureRecipe_RaiderDen_Shelves_04",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Res Armchair 01 A",
              "technical": "DA_Item_OutpostFurnitureRecipe_Res_Armchair_01_A",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Res Ceiling Lamp 01 A",
              "technical": "DA_Item_OutpostFurnitureRecipe_Res_CeilingLamp_01_A",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Res Chair 02 A",
              "technical": "DA_Item_OutpostFurnitureRecipe_Res_Chair_02_A",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Res Chair 03 A",
              "technical": "DA_Item_OutpostFurnitureRecipe_Res_Chair_03_A",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Res Drawer 01 A",
              "technical": "DA_Item_OutpostFurnitureRecipe_Res_Drawer_01_A",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Res Drawer Medical 01 A",
              "technical": "DA_Item_OutpostFurnitureRecipe_Res_DrawerMedical_01_A",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Res Piano 01 A",
              "technical": "DA_Item_OutpostFurnitureRecipe_Res_Piano_01_A",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Res Sofa 01 Y150 A",
              "technical": "DA_Item_OutpostFurnitureRecipe_Res_Sofa_01_Y150_A",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Res Sofa 01 Y150 A Patterns",
              "technical": "DA_Item_OutpostFurnitureRecipe_Res_Sofa_01_Y150_A_Patterns",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Res Sofa 01 Y150 A Patterns Geo",
              "technical": "DA_Item_OutpostFurnitureRecipe_Res_Sofa_01_Y150_A_Patterns_Geo",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Res Sofa 01 Y150 A Patterns Green Grometry",
              "technical": "DA_Item_OutpostFurnitureRecipe_Res_Sofa_01_Y150_A_Patterns_Green_Grometry",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Res Sofa 01 Y150 A Research Company BC",
              "technical": "DA_Item_OutpostFurnitureRecipe_Res_Sofa_01_Y150_A_ResearchCompany_BC",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Res Sofa 01 Y300 A",
              "technical": "DA_Item_OutpostFurnitureRecipe_Res_Sofa_01_Y300_A",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Res Sofa 01 Y300 A Patterns",
              "technical": "DA_Item_OutpostFurnitureRecipe_Res_Sofa_01_Y300_A_Patterns",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Res Sofa 01 Y300 A Patterns Geo",
              "technical": "DA_Item_OutpostFurnitureRecipe_Res_Sofa_01_Y300_A_Patterns_Geo",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Res Sofa 01 Y300 A Patterns Green Grometry",
              "technical": "DA_Item_OutpostFurnitureRecipe_Res_Sofa_01_Y300_A_Patterns_Green_Grometry",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Res Sofa 01 Y300 A Research Company BC",
              "technical": "DA_Item_OutpostFurnitureRecipe_Res_Sofa_01_Y300_A_ResearchCompany_BC",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Res Sofa 03 A",
              "technical": "DA_Item_OutpostFurnitureRecipe_Res_Sofa_03_A",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Res Table 01 A",
              "technical": "DA_Item_OutpostFurnitureRecipe_Res_Table_01_A",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Res Table 02 A",
              "technical": "DA_Item_OutpostFurnitureRecipe_Res_Table_02_A",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Res Table 03 A",
              "technical": "DA_Item_OutpostFurnitureRecipe_Res_Table_03_A",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Rsr Cabinet M 02 X100cm B",
              "technical": "DA_Item_OutpostFurnitureRecipe_Rsr_Cabinet_M_02_X100cm_B",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Rsr Cabinet M 02 X200cm B",
              "technical": "DA_Item_OutpostFurnitureRecipe_Rsr_Cabinet_M_02_X200cm_B",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Rsr Cabinet S 01 B",
              "technical": "DA_Item_OutpostFurnitureRecipe_Rsr_Cabinet_S_01_B",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Rsr Cabinet S 02 A Blue",
              "technical": "DA_Item_OutpostFurnitureRecipe_Rsr_Cabinet_S_02_A_Blue",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Rsr Ceiling Light 01 A",
              "technical": "DA_Item_OutpostFurnitureRecipe_Rsr_CeilingLight_01_A",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Rsr Chair 01 A",
              "technical": "DA_Item_OutpostFurnitureRecipe_Rsr_Chair_01_A",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Rsr Chair 02 A",
              "technical": "DA_Item_OutpostFurnitureRecipe_Rsr_Chair_02_A",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Rsr Stool 01 A",
              "technical": "DA_Item_OutpostFurnitureRecipe_Rsr_Stool_01_A",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe SM Res Closet 01 A",
              "technical": "DA_Item_OutpostFurnitureRecipe_SM_Res_Closet_01_A",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe SM Res Tv Unit 01 A",
              "technical": "DA_Item_OutpostFurnitureRecipe_SM_Res_TvUnit_01_A",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe SM Rsr Cabinet M 02 X100cm A",
              "technical": "DA_Item_OutpostFurnitureRecipe_SM_Rsr_Cabinet_M_02_X100cm_A",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe SM Rsr Cabinet M 02 X200cm A",
              "technical": "DA_Item_OutpostFurnitureRecipe_SM_Rsr_Cabinet_M_02_X200cm_A",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe SM Wrh Desk 01 A",
              "technical": "DA_Item_OutpostFurnitureRecipe_SM_Wrh_Desk_01_A",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Sct Chair 01 A",
              "technical": "DA_Item_OutpostFurnitureRecipe_Sct_Chair_01_A",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Shelves 02 A",
              "technical": "DA_Item_OutpostFurnitureRecipe_Shelves_02_A",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Shelves 02 B",
              "technical": "DA_Item_OutpostFurnitureRecipe_Shelves_02_B",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Sofa 01 A",
              "technical": "DA_Item_OutpostFurnitureRecipe_Sofa_01_A",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe South Forest Bush Holly 01 C",
              "technical": "DA_Item_OutpostFurnitureRecipe_South_Forest_Bush_Holly_01_C",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Stash 01 A",
              "technical": "DA_Item_OutpostFurnitureRecipe_Stash_01_A",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Stash 01 B",
              "technical": "DA_Item_OutpostFurnitureRecipe_Stash_01_B",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Stash 01 C",
              "technical": "DA_Item_OutpostFurnitureRecipe_Stash_01_C",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Stash 01 D",
              "technical": "DA_Item_OutpostFurnitureRecipe_Stash_01_D",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Venetian Dresser 01 A",
              "technical": "DA_Item_OutpostFurnitureRecipe_VenetianDresser_01_A",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Wardrobe 01 B",
              "technical": "DA_Item_OutpostFurnitureRecipe_Wardrobe_01_B",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Wardrobe 01 C",
              "technical": "DA_Item_OutpostFurnitureRecipe_Wardrobe_01_C",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Wardrobe 01 D",
              "technical": "DA_Item_OutpostFurnitureRecipe_Wardrobe_01_D",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Weapon Display 01 A",
              "technical": "DA_Item_OutpostFurnitureRecipe_WeaponDisplay_01_A",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Weapon Display 01 B",
              "technical": "DA_Item_OutpostFurnitureRecipe_WeaponDisplay_01_B",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Weapon Display 01 C",
              "technical": "DA_Item_OutpostFurnitureRecipe_WeaponDisplay_01_C",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Weather Monitor 01 Chair",
              "technical": "DA_Item_OutpostFurnitureRecipe_WeatherMonitor_01_Chair",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Weather Monitor 01 Table",
              "technical": "DA_Item_OutpostFurnitureRecipe_WeatherMonitor_01_Table",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Wrh Chair 01 A",
              "technical": "DA_Item_OutpostFurnitureRecipe_Wrh_Chair_01_A",
              "status": "unassessed"
            },
            {
              "name": "Outpost Furniture Recipe Wrh Drawers 01 A",
              "technical": "DA_Item_OutpostFurnitureRecipe_Wrh_Drawers_01_A",
              "status": "unassessed"
            }
          ]
        },
        {
          "name": "Blueprints",
          "children": [
            {
              "name": "Recipe Advanced Camera Recipe",
              "technical": "DA_Item_Recipe_AdvancedCameraRecipe",
              "status": "unassessed"
            },
            {
              "name": "Recipe Assault Rifle Heavy Recipe",
              "technical": "DA_Item_Recipe_AssaultRifleHeavyRecipe",
              "status": "unassessed"
            },
            {
              "name": "Recipe Banjo",
              "technical": "DA_Item_Recipe_Banjo",
              "status": "unassessed"
            },
            {
              "name": "Recipe Battlerifle Light01Recipe",
              "technical": "DA_Item_Recipe_BattlerifleLight01Recipe",
              "status": "unassessed"
            },
            {
              "name": "Recipe Blue Glowstick Recipe",
              "technical": "DA_Item_Recipe_BlueGlowstickRecipe",
              "status": "unassessed"
            },
            {
              "name": "Recipe Canto Recipe",
              "technical": "DA_Item_Recipe_CantoRecipe",
              "status": "unassessed"
            },
            {
              "name": "Recipe Charge Rifle Recipe",
              "technical": "DA_Item_Recipe_ChargeRifleRecipe",
              "status": "unassessed"
            },
            {
              "name": "Recipe Collector Key",
              "technical": "DA_Item_Recipe_CollectorKey",
              "status": "unassessed"
            },
            {
              "name": "Recipe Combat Agument IIIFlanking Recipe",
              "technical": "DA_Item_Recipe_CombatAgumentIIIFlankingRecipe",
              "status": "unassessed"
            },
            {
              "name": "Recipe Combat Agument T3Aggressive Recipe",
              "technical": "DA_Item_Recipe_CombatAgumentT3AggressiveRecipe",
              "status": "unassessed"
            },
            {
              "name": "Recipe Complex Gun Parts Recipe",
              "technical": "DA_Item_Recipe_ComplexGunPartsRecipe",
              "status": "unassessed"
            },
            {
              "name": "Recipe Crash Pad Recipe",
              "technical": "DA_Item_Recipe_CrashPadRecipe",
              "status": "unassessed"
            },
            {
              "name": "Recipe Dolabra Recipe",
              "technical": "DA_Item_Recipe_DolabraRecipe",
              "status": "unassessed"
            },
            {
              "name": "Recipe Extended Barrel02Recipe",
              "technical": "DA_Item_Recipe_ExtendedBarrel02Recipe",
              "status": "unassessed"
            },
            {
              "name": "Recipe Fireworks Box Blueprint",
              "technical": "DA_Item_Recipe_FireworksBoxBlueprint",
              "status": "unassessed"
            },
            {
              "name": "Recipe Gas Mine Recipe",
              "technical": "DA_Item_Recipe_GasMineRecipe",
              "status": "unassessed"
            },
            {
              "name": "Recipe Grappling Hook Recipe",
              "technical": "DA_Item_Recipe_GrapplingHookRecipe",
              "status": "unassessed"
            },
            {
              "name": "Recipe Green Glowstick Recipe",
              "technical": "DA_Item_Recipe_GreenGlowstickRecipe",
              "status": "unassessed"
            },
            {
              "name": "Recipe Impulse Mine Recipe",
              "technical": "DA_Item_Recipe_ImpulseMineRecipe",
              "status": "unassessed"
            },
            {
              "name": "Recipe Item Skin Banshee",
              "technical": "DA_Item_Recipe_ItemSkin_Banshee",
              "status": "unassessed"
            },
            {
              "name": "Recipe Item Skin Bells",
              "technical": "DA_Item_Recipe_ItemSkin_Bells",
              "status": "unassessed"
            },
            {
              "name": "Recipe Item Skin Common Blue",
              "technical": "DA_Item_Recipe_ItemSkin_CommonBlue",
              "status": "unassessed"
            },
            {
              "name": "Recipe Item Skin Common Red",
              "technical": "DA_Item_Recipe_ItemSkin_CommonRed",
              "status": "unassessed"
            },
            {
              "name": "Recipe Item Skin Common Yellow",
              "technical": "DA_Item_Recipe_ItemSkin_CommonYellow",
              "status": "unassessed"
            },
            {
              "name": "Recipe Item Skin Corto01",
              "technical": "DA_Item_Recipe_ItemSkin_Corto01",
              "status": "unassessed"
            },
            {
              "name": "Recipe Item Skin Dust Stroke Camo",
              "technical": "DA_Item_Recipe_ItemSkin_DustStrokeCamo",
              "status": "unassessed"
            },
            {
              "name": "Recipe Item Skin Mural Tree",
              "technical": "DA_Item_Recipe_ItemSkin_MuralTree",
              "status": "unassessed"
            },
            {
              "name": "Recipe Item Skin Organic Island",
              "technical": "DA_Item_Recipe_ItemSkin_OrganicIsland",
              "status": "unassessed"
            },
            {
              "name": "Recipe Item Skin Outlander",
              "technical": "DA_Item_Recipe_ItemSkin_Outlander",
              "status": "unassessed"
            },
            {
              "name": "Recipe Item Skin Renzo",
              "technical": "DA_Item_Recipe_ItemSkin_Renzo",
              "status": "unassessed"
            },
            {
              "name": "Recipe Item Skin Station Master",
              "technical": "DA_Item_Recipe_ItemSkin_StationMaster",
              "status": "unassessed"
            },
            {
              "name": "Recipe Item Skin Tortoise01",
              "technical": "DA_Item_Recipe_ItemSkin_Tortoise01",
              "status": "unassessed"
            },
            {
              "name": "Recipe Item Skin Yeti",
              "technical": "DA_Item_Recipe_ItemSkin_Yeti",
              "status": "unassessed"
            },
            {
              "name": "Recipe Launcher Anti ARCSingle Shot01Recipe",
              "technical": "DA_Item_Recipe_LauncherAntiARCSingleShot01Recipe",
              "status": "unassessed"
            },
            {
              "name": "Recipe Lone Wolf Recipe",
              "technical": "DA_Item_Recipe_LoneWolfRecipe",
              "status": "unassessed"
            },
            {
              "name": "Recipe Looting Agument IIIDBNORecipe",
              "technical": "DA_Item_Recipe_LootingAgumentIIIDBNORecipe",
              "status": "unassessed"
            },
            {
              "name": "Recipe Looting Augment T3Custodian Recipe",
              "technical": "DA_Item_Recipe_LootingAugmentT3CustodianRecipe",
              "status": "unassessed"
            },
            {
              "name": "Recipe Powered Descender Recipe",
              "technical": "DA_Item_Recipe_PoweredDescenderRecipe",
              "status": "unassessed"
            },
            {
              "name": "Recipe Recipe Angled Grip 02",
              "technical": "DA_Item_Recipe_Recipe_AngledGrip_02",
              "status": "unassessed"
            },
            {
              "name": "Recipe Recipe Angled Grip 03",
              "technical": "DA_Item_Recipe_Recipe_AngledGrip_03",
              "status": "unassessed"
            },
            {
              "name": "Recipe Recipe Anti ARCLauncher",
              "technical": "DA_Item_Recipe_Recipe_AntiARCLauncher",
              "status": "unassessed"
            },
            {
              "name": "Recipe Recipe Anvil",
              "technical": "DA_Item_Recipe_Recipe_Anvil",
              "status": "unassessed"
            },
            {
              "name": "Recipe Recipe Assault Rifle Burst",
              "technical": "DA_Item_Recipe_Recipe_AssaultRifleBurst",
              "status": "unassessed"
            },
            {
              "name": "Recipe Recipe Assault Rifle Low Tier",
              "technical": "DA_Item_Recipe_Recipe_AssaultRifleLowTier",
              "status": "unassessed"
            },
            {
              "name": "Recipe Recipe Barricade Kit",
              "technical": "DA_Item_Recipe_Recipe_BarricadeKit",
              "status": "unassessed"
            },
            {
              "name": "Recipe Recipe Blaze Grenade",
              "technical": "DA_Item_Recipe_Recipe_BlazeGrenade",
              "status": "unassessed"
            },
            {
              "name": "Recipe Recipe Bobcat",
              "technical": "DA_Item_Recipe_Recipe_Bobcat",
              "status": "unassessed"
            },
            {
              "name": "Recipe Recipe Bolt Action Medium",
              "technical": "DA_Item_Recipe_Recipe_BoltActionMedium",
              "status": "unassessed"
            },
            {
              "name": "Recipe Recipe Burletta",
              "technical": "DA_Item_Recipe_Recipe_Burletta",
              "status": "unassessed"
            },
            {
              "name": "Recipe Recipe Compensator 02",
              "technical": "DA_Item_Recipe_Recipe_Compensator_02",
              "status": "unassessed"
            },
            {
              "name": "Recipe Recipe Compensator 03",
              "technical": "DA_Item_Recipe_Recipe_Compensator_03",
              "status": "unassessed"
            },
            {
              "name": "Recipe Recipe Defib",
              "technical": "DA_Item_Recipe_Recipe_Defib",
              "status": "unassessed"
            },
            {
              "name": "Recipe Recipe Door Blocker",
              "technical": "DA_Item_Recipe_Recipe_DoorBlocker",
              "status": "unassessed"
            },
            {
              "name": "Recipe Recipe Equlizer",
              "technical": "DA_Item_Recipe_Recipe_Equlizer",
              "status": "unassessed"
            },
            {
              "name": "Recipe Recipe Extended Barrel",
              "technical": "DA_Item_Recipe_Recipe_ExtendedBarrel",
              "status": "unassessed"
            },
            {
              "name": "Recipe Recipe Extended Light Mag 02",
              "technical": "DA_Item_Recipe_Recipe_ExtendedLightMag_02",
              "status": "unassessed"
            },
            {
              "name": "Recipe Recipe Extended Light Mag 03",
              "technical": "DA_Item_Recipe_Recipe_ExtendedLightMag_03",
              "status": "unassessed"
            },
            {
              "name": "Recipe Recipe Extended Medium Mag 02",
              "technical": "DA_Item_Recipe_Recipe_ExtendedMediumMag_02",
              "status": "unassessed"
            },
            {
              "name": "Recipe Recipe Extended Medium Mag 03",
              "technical": "DA_Item_Recipe_Recipe_ExtendedMediumMag_03",
              "status": "unassessed"
            },
            {
              "name": "Recipe Recipe Extended Shotgun Mag 02",
              "technical": "DA_Item_Recipe_Recipe_ExtendedShotgunMag_02",
              "status": "unassessed"
            },
            {
              "name": "Recipe Recipe Extended Shotgun Mag 03",
              "technical": "DA_Item_Recipe_Recipe_ExtendedShotgunMag_03",
              "status": "unassessed"
            },
            {
              "name": "Recipe Recipe Flare Trap",
              "technical": "DA_Item_Recipe_Recipe_FlareTrap",
              "status": "unassessed"
            },
            {
              "name": "Recipe Recipe Heavy Gun Parts",
              "technical": "DA_Item_Recipe_Recipe_HeavyGunParts",
              "status": "unassessed"
            },
            {
              "name": "Recipe Recipe Hi Power Pistol",
              "technical": "DA_Item_Recipe_Recipe_HiPowerPistol",
              "status": "unassessed"
            },
            {
              "name": "Recipe Recipe Il Toro",
              "technical": "DA_Item_Recipe_Recipe_IlToro",
              "status": "unassessed"
            },
            {
              "name": "Recipe Recipe Jolt Mine",
              "technical": "DA_Item_Recipe_Recipe_JoltMine",
              "status": "unassessed"
            },
            {
              "name": "Recipe Recipe Jump Mine",
              "technical": "DA_Item_Recipe_Recipe_JumpMine",
              "status": "unassessed"
            },
            {
              "name": "Recipe Recipe Jupiter",
              "technical": "DA_Item_Recipe_Recipe_Jupiter",
              "status": "unassessed"
            },
            {
              "name": "Recipe Recipe Light Gun Parts",
              "technical": "DA_Item_Recipe_Recipe_LightGunParts",
              "status": "unassessed"
            },
            {
              "name": "Recipe Recipe Lightweight Stock",
              "technical": "DA_Item_Recipe_Recipe_LightweightStock",
              "status": "unassessed"
            },
            {
              "name": "Recipe Recipe Lure Grenade",
              "technical": "DA_Item_Recipe_Recipe_LureGrenade",
              "status": "unassessed"
            },
            {
              "name": "Recipe Recipe Medium Gun Parts",
              "technical": "DA_Item_Recipe_Recipe_MediumGunParts",
              "status": "unassessed"
            },
            {
              "name": "Recipe Recipe Medium LMG",
              "technical": "DA_Item_Recipe_Recipe_MediumLMG",
              "status": "unassessed"
            },
            {
              "name": "Recipe Recipe Muzzle Brake 02",
              "technical": "DA_Item_Recipe_Recipe_MuzzleBrake_02",
              "status": "unassessed"
            },
            {
              "name": "Recipe Recipe Muzzle Brake 03",
              "technical": "DA_Item_Recipe_Recipe_MuzzleBrake_03",
              "status": "unassessed"
            },
            {
              "name": "Recipe Recipe Padded Stock",
              "technical": "DA_Item_Recipe_Recipe_PaddedStock",
              "status": "unassessed"
            },
            {
              "name": "Recipe Recipe Shotgun Choke 02",
              "technical": "DA_Item_Recipe_Recipe_ShotgunChoke_02",
              "status": "unassessed"
            },
            {
              "name": "Recipe Recipe Shotgun Choke 03",
              "technical": "DA_Item_Recipe_Recipe_ShotgunChoke_03",
              "status": "unassessed"
            },
            {
              "name": "Recipe Recipe Shotgun Silencer",
              "technical": "DA_Item_Recipe_Recipe_ShotgunSilencer",
              "status": "unassessed"
            },
            {
              "name": "Recipe Recipe Showstopper",
              "technical": "DA_Item_Recipe_Recipe_Showstopper",
              "status": "unassessed"
            },
            {
              "name": "Recipe Recipe Silencer 01",
              "technical": "DA_Item_Recipe_Recipe_Silencer_01",
              "status": "unassessed"
            },
            {
              "name": "Recipe Recipe Silencer 02",
              "technical": "DA_Item_Recipe_Recipe_Silencer_02",
              "status": "unassessed"
            },
            {
              "name": "Recipe Recipe Snap Hook",
              "technical": "DA_Item_Recipe_Recipe_SnapHook",
              "status": "unassessed"
            },
            {
              "name": "Recipe Recipe Stable Stock 02",
              "technical": "DA_Item_Recipe_Recipe_StableStock_02",
              "status": "unassessed"
            },
            {
              "name": "Recipe Recipe Stable Stock 03",
              "technical": "DA_Item_Recipe_Recipe_StableStock_03",
              "status": "unassessed"
            },
            {
              "name": "Recipe Recipe Tagging Grenade",
              "technical": "DA_Item_Recipe_Recipe_TaggingGrenade",
              "status": "unassessed"
            },
            {
              "name": "Recipe Recipe Tempest",
              "technical": "DA_Item_Recipe_Recipe_Tempest",
              "status": "unassessed"
            },
            {
              "name": "Recipe Recipe Trigger Nade",
              "technical": "DA_Item_Recipe_Recipe_TriggerNade",
              "status": "unassessed"
            },
            {
              "name": "Recipe Recipe Vertical Grip 02",
              "technical": "DA_Item_Recipe_Recipe_VerticalGrip_02",
              "status": "unassessed"
            },
            {
              "name": "Recipe Recipe Vertical Grip 03",
              "technical": "DA_Item_Recipe_Recipe_VerticalGrip_03",
              "status": "unassessed"
            },
            {
              "name": "Recipe Recipe Vita Shot",
              "technical": "DA_Item_Recipe_Recipe_VitaShot",
              "status": "unassessed"
            },
            {
              "name": "Recipe Recipe Vita Spray",
              "technical": "DA_Item_Recipe_Recipe_VitaSpray",
              "status": "unassessed"
            },
            {
              "name": "Recipe Recipe Vulcano",
              "technical": "DA_Item_Recipe_Recipe_Vulcano",
              "status": "unassessed"
            },
            {
              "name": "Recipe Recipe Wolfpack",
              "technical": "DA_Item_Recipe_Recipe_Wolfpack",
              "status": "unassessed"
            },
            {
              "name": "Recipe Red Glowstick Recipe",
              "technical": "DA_Item_Recipe_RedGlowstickRecipe",
              "status": "unassessed"
            },
            {
              "name": "Recipe Smoke Grenade Recipe",
              "technical": "DA_Item_Recipe_SmokeGrenadeRecipe",
              "status": "unassessed"
            },
            {
              "name": "Recipe Snubnose01Recipe",
              "technical": "DA_Item_Recipe_Snubnose01Recipe",
              "status": "unassessed"
            },
            {
              "name": "Recipe Surge Coil Recipe",
              "technical": "DA_Item_Recipe_SurgeCoilRecipe",
              "status": "unassessed"
            },
            {
              "name": "Recipe Tactical Augment T3Revival Recipe",
              "technical": "DA_Item_Recipe_TacticalAugmentT3RevivalRecipe",
              "status": "unassessed"
            },
            {
              "name": "Recipe Tether Grenade Recipe",
              "technical": "DA_Item_Recipe_TetherGrenadeRecipe",
              "status": "unassessed"
            },
            {
              "name": "Recipe Tether Gun Recipe",
              "technical": "DA_Item_Recipe_TetherGunRecipe",
              "status": "unassessed"
            },
            {
              "name": "Recipe Timed Mine Recipe",
              "technical": "DA_Item_Recipe_TimedMineRecipe",
              "status": "unassessed"
            },
            {
              "name": "Recipe Trail Blazer Recipe",
              "technical": "DA_Item_Recipe_TrailBlazerRecipe",
              "status": "unassessed"
            },
            {
              "name": "Recipe Utility Agument T3Defensive Recipe",
              "technical": "DA_Item_Recipe_UtilityAgumentT3DefensiveRecipe",
              "status": "unassessed"
            },
            {
              "name": "Recipe Utility Agument T3Healing Recipe",
              "technical": "DA_Item_Recipe_UtilityAgumentT3HealingRecipe",
              "status": "unassessed"
            },
            {
              "name": "Recipe Utility Augment T3Smoke Recipe",
              "technical": "DA_Item_Recipe_UtilityAugmentT3SmokeRecipe",
              "status": "unassessed"
            },
            {
              "name": "Recipe White Flag Recipe",
              "technical": "DA_Item_Recipe_WhiteFlagRecipe",
              "status": "unassessed"
            },
            {
              "name": "Recipe Yellow Glow Stick Recipe",
              "technical": "DA_Item_Recipe_YellowGlowStickRecipe",
              "status": "unassessed"
            }
          ]
        },
        {
          "name": "Weapon mods",
          "children": [
            {
              "name": "Weapon Mod Magazine Light Extended 01",
              "technical": "DA_Item_WeaponMod_MagazineLight_Extended_01",
              "status": "unassessed"
            },
            {
              "name": "Weapon Mod Magazine Light Extended 02",
              "technical": "DA_Item_WeaponMod_MagazineLight_Extended_02",
              "status": "unassessed"
            },
            {
              "name": "Weapon Mod Magazine Light Extended 03",
              "technical": "DA_Item_WeaponMod_MagazineLight_Extended_03",
              "status": "unassessed"
            },
            {
              "name": "Weapon Mod Magazine Medium Extended 01",
              "technical": "DA_Item_WeaponMod_MagazineMedium_Extended_01",
              "status": "unassessed"
            },
            {
              "name": "Weapon Mod Magazine Medium Extended 02",
              "technical": "DA_Item_WeaponMod_MagazineMedium_Extended_02",
              "status": "unassessed"
            },
            {
              "name": "Weapon Mod Magazine Medium Extended 03",
              "technical": "DA_Item_WeaponMod_MagazineMedium_Extended_03",
              "status": "unassessed"
            },
            {
              "name": "Weapon Mod Magazine Shotgun Extended 01",
              "technical": "DA_Item_WeaponMod_MagazineShotgun_Extended_01",
              "status": "unassessed"
            },
            {
              "name": "Weapon Mod Magazine Shotgun Extended 02",
              "technical": "DA_Item_WeaponMod_MagazineShotgun_Extended_02",
              "status": "unassessed"
            },
            {
              "name": "Weapon Mod Magazine Shotgun Extended 03",
              "technical": "DA_Item_WeaponMod_MagazineShotgun_Extended_03",
              "status": "unassessed"
            },
            {
              "name": "Weapon Mod Muzzle Brake 01",
              "technical": "DA_Item_WeaponMod_Muzzle_Brake_01",
              "status": "unassessed"
            },
            {
              "name": "Weapon Mod Muzzle Brake 02",
              "technical": "DA_Item_WeaponMod_Muzzle_Brake_02",
              "status": "unassessed"
            },
            {
              "name": "Weapon Mod Muzzle Brake 03",
              "technical": "DA_Item_WeaponMod_Muzzle_Brake_03",
              "status": "unassessed"
            },
            {
              "name": "Weapon Mod Muzzle Compensator 01",
              "technical": "DA_Item_WeaponMod_Muzzle_Compensator_01",
              "status": "unassessed"
            },
            {
              "name": "Weapon Mod Muzzle Compensator 02",
              "technical": "DA_Item_WeaponMod_Muzzle_Compensator_02",
              "status": "unassessed"
            },
            {
              "name": "Weapon Mod Muzzle Compensator 03",
              "technical": "DA_Item_WeaponMod_Muzzle_Compensator_03",
              "status": "unassessed"
            },
            {
              "name": "Weapon Mod Muzzle Extended Barrel 01",
              "technical": "DA_Item_WeaponMod_Muzzle_ExtendedBarrel_01",
              "status": "unassessed"
            },
            {
              "name": "Weapon Mod Muzzle Extended Barrel 02",
              "technical": "DA_Item_WeaponMod_Muzzle_ExtendedBarrel_02",
              "status": "unassessed"
            },
            {
              "name": "Weapon Mod Muzzle Extended Barrel 03",
              "technical": "DA_Item_WeaponMod_Muzzle_ExtendedBarrel_03",
              "status": "unassessed"
            },
            {
              "name": "Weapon Mod Muzzle Silencer 01",
              "technical": "DA_Item_WeaponMod_Muzzle_Silencer_01",
              "status": "unassessed"
            },
            {
              "name": "Weapon Mod Muzzle Silencer 02",
              "technical": "DA_Item_WeaponMod_Muzzle_Silencer_02",
              "status": "unassessed"
            },
            {
              "name": "Weapon Mod Muzzle Silencer 03",
              "technical": "DA_Item_WeaponMod_Muzzle_Silencer_03",
              "status": "unassessed"
            },
            {
              "name": "Weapon Mod Shotgun Muzzle Choke 01",
              "technical": "DA_Item_WeaponMod_ShotgunMuzzle_Choke_01",
              "status": "unassessed"
            },
            {
              "name": "Weapon Mod Shotgun Muzzle Choke 02",
              "technical": "DA_Item_WeaponMod_ShotgunMuzzle_Choke_02",
              "status": "unassessed"
            },
            {
              "name": "Weapon Mod Shotgun Muzzle Choke 03",
              "technical": "DA_Item_WeaponMod_ShotgunMuzzle_Choke_03",
              "status": "unassessed"
            },
            {
              "name": "Weapon Mod Shotgun Muzzle Silencer 01",
              "technical": "DA_Item_WeaponMod_ShotgunMuzzle_Silencer_01",
              "status": "unassessed"
            },
            {
              "name": "Weapon Mod Stock Active Stabilizer 01",
              "technical": "DA_Item_WeaponMod_Stock_ActiveStabilizer_01",
              "status": "unassessed"
            },
            {
              "name": "Weapon Mod Stock Lightweight 01",
              "technical": "DA_Item_WeaponMod_Stock_Lightweight_01",
              "status": "unassessed"
            },
            {
              "name": "Weapon Mod Stock Padded 01",
              "technical": "DA_Item_WeaponMod_Stock_Padded_01",
              "status": "unassessed"
            },
            {
              "name": "Weapon Mod Stock Stable 01",
              "technical": "DA_Item_WeaponMod_Stock_Stable_01",
              "status": "unassessed"
            },
            {
              "name": "Weapon Mod Stock Stable 02",
              "technical": "DA_Item_WeaponMod_Stock_Stable_02",
              "status": "unassessed"
            },
            {
              "name": "Weapon Mod Stock Stable 03",
              "technical": "DA_Item_WeaponMod_Stock_Stable_03",
              "status": "unassessed"
            },
            {
              "name": "Weapon Mod Tech Projectile Splitter",
              "technical": "DA_Item_WeaponMod_Tech_ProjectileSplitter",
              "status": "unassessed"
            },
            {
              "name": "Weapon Mod Under Barrel Angled Grip 01",
              "technical": "DA_Item_WeaponMod_UnderBarrel_AngledGrip_01",
              "status": "unassessed"
            },
            {
              "name": "Weapon Mod Under Barrel Angled Grip 02",
              "technical": "DA_Item_WeaponMod_UnderBarrel_AngledGrip_02",
              "status": "unassessed"
            },
            {
              "name": "Weapon Mod Under Barrel Angled Grip 03",
              "technical": "DA_Item_WeaponMod_UnderBarrel_AngledGrip_03",
              "status": "unassessed"
            },
            {
              "name": "Weapon Mod Under Barrel Horizontal Grip 01",
              "technical": "DA_Item_WeaponMod_UnderBarrel_HorizontalGrip_01",
              "status": "unassessed"
            },
            {
              "name": "Weapon Mod Under Barrel Vertical Grip 01",
              "technical": "DA_Item_WeaponMod_UnderBarrel_VerticalGrip_01",
              "status": "unassessed"
            },
            {
              "name": "Weapon Mod Under Barrel Vertical Grip 02",
              "technical": "DA_Item_WeaponMod_UnderBarrel_VerticalGrip_02",
              "status": "unassessed"
            },
            {
              "name": "Weapon Mod Under Barrel Vertical Grip 03",
              "technical": "DA_Item_WeaponMod_UnderBarrel_VerticalGrip_03",
              "status": "unassessed"
            }
          ]
        },
        {
          "name": "Augments",
          "children": [
            {
              "name": "Augment Combat T2",
              "technical": "DA_Item_Augment_Combat_T2",
              "status": "unassessed"
            },
            {
              "name": "Augment Combat T3 Aggressive",
              "technical": "DA_Item_Augment_Combat_T3_Aggressive",
              "status": "unassessed"
            },
            {
              "name": "Augment Combat T3 Flanking",
              "technical": "DA_Item_Augment_Combat_T3_Flanking",
              "status": "unassessed"
            },
            {
              "name": "Augment Looting Free Loadout",
              "technical": "DA_Item_Augment_Looting_FreeLoadout",
              "status": "unassessed"
            },
            {
              "name": "Augment Looting T2",
              "technical": "DA_Item_Augment_Looting_T2",
              "status": "unassessed"
            },
            {
              "name": "Augment Looting T3 Cautious",
              "technical": "DA_Item_Augment_Looting_T3_Cautious",
              "status": "unassessed"
            },
            {
              "name": "Augment Looting T3 DBNO",
              "technical": "DA_Item_Augment_Looting_T3_DBNO",
              "status": "unassessed"
            },
            {
              "name": "Augment Raider T4 Revival",
              "technical": "DA_Item_Augment_Raider_T4_Revival",
              "status": "unassessed"
            },
            {
              "name": "Augment Raider T4 Safekeeper",
              "technical": "DA_Item_Augment_Raider_T4_Safekeeper",
              "status": "unassessed"
            },
            {
              "name": "Augment Utility T2",
              "technical": "DA_Item_Augment_Utility_T2",
              "status": "unassessed"
            },
            {
              "name": "Augment Utility T3 Defensive",
              "technical": "DA_Item_Augment_Utility_T3_Defensive",
              "status": "unassessed"
            },
            {
              "name": "Augment Utility T3 Healing",
              "technical": "DA_Item_Augment_Utility_T3_Healing",
              "status": "unassessed"
            },
            {
              "name": "Augment Utility T3 Smoke",
              "technical": "DA_Item_Augment_Utility_T3_Smoke",
              "status": "unassessed"
            },
            {
              "name": "Combat Mk.1",
              "technical": "DA_Item_Augment_Combat_T1",
              "status": "partial",
              "note": "Persistent menu selection and Raid frame, capacity and weight support; it comes home from a Dam extraction and is lost on surrender; the full raid lifecycle remains."
            },
            {
              "name": "Looting Mk.1",
              "technical": "DA_Item_Augment_Looting_T1",
              "status": "partial",
              "note": "Persistent menu selection and Raid frame, capacity and weight support; it comes home from a Dam extraction and is lost on surrender; the full raid lifecycle remains."
            },
            {
              "name": "Tactical / Utility Mk.1",
              "technical": "DA_Item_Augment_Utility_T1",
              "status": "partial",
              "note": "Persistent menu selection and Raid frame, capacity and weight support; it comes home from a Dam extraction and is lost on surrender; the full raid lifecycle remains."
            }
          ]
        }
      ]
    },
    {
      "name": "World objects",
      "children": [
        {
          "name": "Loot containers",
          "children": [
            {
              "name": "Torch Ginger",
              "technical": "DA_LootContainer_TorchGinger",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Apricot",
              "technical": "DA_LootContainer_Apricot",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Lemon Tree",
              "technical": "DA_LootContainer_LemonTree",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Olive",
              "technical": "DA_LootContainer_Olive",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Candle Berries",
              "technical": "DA_LootContainer_CandleBerries",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Rover 01",
              "technical": "DA_LootContainer_Rover_01",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Rocketparts 01 A Lid",
              "technical": "DA_LootContainer_Rocketparts_01_A_Lid",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Rocketparts 01 B Lid",
              "technical": "DA_LootContainer_Rocketparts_01_B_Lid",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Rocketparts 01 C Lid",
              "technical": "DA_LootContainer_Rocketparts_01_C_Lid",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Rocketparts 02 Lid 01",
              "technical": "DA_LootContainer_Rocketparts_02_Lid_01",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Rocketparts 02 Lid 02",
              "technical": "DA_LootContainer_Rocketparts_02_Lid_02",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Support Structure Small 01",
              "technical": "DA_LootContainer_SupportStructure_Small_01",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Air Compressor 01 A",
              "technical": "DA_LootContainer_AirCompressor_01_A",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Analog Display 01 A",
              "technical": "DA_LootContainer_AnalogDisplay_01_A",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Antenna Set 01 Lid 01 A",
              "technical": "DA_LootContainer_AntennaSet_01_Lid_01_A",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Bird Nest A",
              "technical": "DA_LootContainer_BirdNest_A",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Bird Nest B",
              "technical": "DA_LootContainer_BirdNest_B",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Bird Nest C",
              "technical": "DA_LootContainer_BirdNest_C",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Bus 01 Lid 01",
              "technical": "DA_LootContainer_Bus_01_Lid_01",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Bus 01 Lid 02",
              "technical": "DA_LootContainer_Bus_01_Lid_02",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Car 01",
              "technical": "DA_LootContainer_Car_01",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Car 02",
              "technical": "DA_LootContainer_Car_02",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Car 03 A",
              "technical": "DA_LootContainer_Car_03_A",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Crate M 01 A",
              "technical": "DA_LootContainer_Crate_M_01_A",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Crate Cart 01 A",
              "technical": "DA_LootContainer_CrateCart_01_A",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Data Storage Container 01 A",
              "technical": "DA_LootContainer_DataStorageContainer_01_A",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Display Time Board 02 A",
              "technical": "DA_LootContainer_DisplayTimeBoard_02_A",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Enemy Wreck 02 A",
              "technical": "DA_LootContainer_EnemyWreck_02_A",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Fruit Basket 01",
              "technical": "DA_LootContainer_FruitBasket_01",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Fuel Silo 01",
              "technical": "DA_LootContainer_FuelSilo_01",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Fuse Box 01 A",
              "technical": "DA_LootContainer_FuseBox_01_A",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Highway Addon Antenna 01",
              "technical": "DA_LootContainer_HighwayAddon_Antenna_01",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Power Generator 01 A",
              "technical": "DA_LootContainer_PowerGenerator_01_A",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Road Barrier 03 Lid 01 A",
              "technical": "DA_LootContainer_RoadBarrier_03_Lid_01_A",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Road Barrier 03 Lid 02 A",
              "technical": "DA_LootContainer_RoadBarrier_03_Lid_02_A",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Road Barrier 03 Lid 03 A",
              "technical": "DA_LootContainer_RoadBarrier_03_Lid_03_A",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Road Barrier 03 Lid 04 A",
              "technical": "DA_LootContainer_RoadBarrier_03_Lid_04_A",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Sct Security Camera 01 A",
              "technical": "DA_LootContainer_Sct_SecurityCamera_01_A",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Solar Panel Hatch 03 A",
              "technical": "DA_LootContainer_SolarPanelHatch_03_A",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Solar Panel Hatch 03 B",
              "technical": "DA_LootContainer_SolarPanelHatch_03_B",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Sosbooth 01 A",
              "technical": "DA_LootContainer_Sosbooth_01_A",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Street Sweeper 01 A",
              "technical": "DA_LootContainer_StreetSweeper_01_A",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Truck Utility 01",
              "technical": "DA_LootContainer_TruckUtility_01",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Vent 03 A",
              "technical": "DA_LootContainer_Vent_03_A",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Vent Machine Large 01 A",
              "technical": "DA_LootContainer_VentMachineLarge_01_A",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Vent Machine Large 02 A",
              "technical": "DA_LootContainer_VentMachineLarge_02_A",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Cmr Cash Register 01 Lid 01 A",
              "technical": "DA_LootContainer_Cmr_CashRegister_01_Lid_01_A",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Cmr Coffee Machine 01 A",
              "technical": "DA_LootContainer_Cmr_CoffeeMachine_01_A",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Cmr Cupboard Cafe 01 Drawer 01 A",
              "technical": "DA_LootContainer_Cmr_CupboardCafe_01_Drawer_01_A",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Cmr Cupboard Cafe 01 Drawer 02 A",
              "technical": "DA_LootContainer_Cmr_CupboardCafe_01_Drawer_02_A",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Cmr Freezer 01 Lid 01 A",
              "technical": "DA_LootContainer_Cmr_Freezer_01_Lid_01_A",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Cmr Fridge L 01 Lid 01 A",
              "technical": "DA_LootContainer_Cmr_Fridge_L_01_Lid_01_A",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Information Terminal 01 Lid 01 A",
              "technical": "DA_LootContainer_InformationTerminal_01_Lid_01_A",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Large Ticket Booth Lid",
              "technical": "DA_LootContainer_LargeTicketBooth_Lid",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Phone Booth Lid",
              "technical": "DA_LootContainer_PhoneBoothLid",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Shrub 03 H",
              "technical": "DA_LootContainer_Shrub_03_H",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Cmr Shelf 03 Drawer C",
              "technical": "DA_LootContainer_Cmr_Shelf_03_Drawer_C",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Stacking Box 01 A",
              "technical": "DA_LootContainer_StackingBox_01_A",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Stacking Box Lid 01 A",
              "technical": "DA_LootContainer_StackingBox_Lid_01_A",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Stacking Box Lid 01 B",
              "technical": "DA_LootContainer_StackingBox_Lid_01_B",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Stacking Box Lid 01 C",
              "technical": "DA_LootContainer_StackingBox_Lid_01_C",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Trash Bin A 01",
              "technical": "DA_LootContainer_TrashBin_A_01",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Vending Machine Door",
              "technical": "DA_LootContainer_VendingMachineDoor",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Cmr Wall Add 01 A Lid 01",
              "technical": "DA_LootContainer_Cmr_WallAdd_01_A_Lid_01",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Cmr Wall Add 01 A Lid 02",
              "technical": "DA_LootContainer_Cmr_WallAdd_01_A_Lid_02",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Cmr Wall Add 03 A Lid 01",
              "technical": "DA_LootContainer_Cmr_WallAdd_03_A_Lid_01",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Coms Console 02 A",
              "technical": "DA_LootContainer_Coms_Console_02_A",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Coms Console 03 A",
              "technical": "DA_LootContainer_Coms_Console_03_A",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Coms Servers 01 A",
              "technical": "DA_LootContainer_Coms_Servers_01_A",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Coms Servers 02 A",
              "technical": "DA_LootContainer_Coms_Servers_02_A",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "CUL Museum Crate 01 A",
              "technical": "DA_LootContainer_CUL_MuseumCrate_01_A",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "CUL Museum Crate 02 A",
              "technical": "DA_LootContainer_CUL_MuseumCrate_02_A",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Fertilizer",
              "technical": "DA_LootContainer_Fertilizer",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Moss",
              "technical": "DA_LootContainer_Moss",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Antenaa XL 01 Lid 01 A",
              "technical": "DA_LootContainer_Antenaa_XL_01_Lid_01_A",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Antenaa XL 01 Lid 02 A",
              "technical": "DA_LootContainer_Antenaa_XL_01_Lid_02_A",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Gvt Bin Trash 01 A",
              "technical": "DA_LootContainer_Gvt_BinTrash_01_A",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Gvt Center Bench M 01 A",
              "technical": "DA_LootContainer_Gvt_CenterBench_M_01_A",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Gvt Computer 01 A",
              "technical": "DA_LootContainer_Gvt_Computer_01_A",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Gvt Computer 03",
              "technical": "DA_LootContainer_Gvt_Computer_03",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Gvt Desk 01",
              "technical": "DA_LootContainer_Gvt_Desk_01",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Floodlight Tower 01 Lid 01 A",
              "technical": "DA_LootContainer_FloodlightTower_01_Lid_01_A",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Floodlight Tower 01 Lid 02 A",
              "technical": "DA_LootContainer_FloodlightTower_01_Lid_02_A",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Generator M 02 Lid 01",
              "technical": "DA_LootContainer_Generator_M_02_Lid_01",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Gvt Generator M 02 A",
              "technical": "DA_LootContainer_Gvt_Generator_M_02_A",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Gvt Machine M 01 A",
              "technical": "DA_LootContainer_Gvt_Machine_M_01_A",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Gvt Machine M 02 A Large Panel",
              "technical": "DA_LootContainer_Gvt_Machine_M_02_A_LargePanel",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Gvt Machine M 02 A Small Panel",
              "technical": "DA_LootContainer_Gvt_Machine_M_02_A_SmallPanel",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Gvt Machine M 04 A Panel Large",
              "technical": "DA_LootContainer_Gvt_Machine_M_04_A_PanelLarge",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Gvt Machine M 04 A Panel Small",
              "technical": "DA_LootContainer_Gvt_Machine_M_04_A_PanelSmall",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Gvt Server 01 x400z250 Door",
              "technical": "DA_LootContainer_Gvt_Server_01_x400z250_Door",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Time Stamp 01 A",
              "technical": "DA_LootContainer_TimeStamp_01_A",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Time Stamp 03 A",
              "technical": "DA_LootContainer_TimeStamp_03_A",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Android 01 Lid A",
              "technical": "DA_LootContainer_Android_01_Lid_A",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Android 01 Lid B",
              "technical": "DA_LootContainer_Android_01_Lid_B",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Arc Payload 01",
              "technical": "DA_LootContainer_ArcPayload_01",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Destroyed Robot 01 Torso Lid A",
              "technical": "DA_LootContainer_Destroyed_Robot_01_TorsoLid_A",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Wasabi Panel",
              "technical": "DA_LootContainer_WasabiPanel",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Raider Weapon Case 01 Lid A",
              "technical": "DA_LootContainer_Raider_WeaponCase_01_Lid_A",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Rsr Cabinet M 01",
              "technical": "DA_LootContainer_Rsr_Cabinet_M_01",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Rsr Cabinet S 01",
              "technical": "DA_LootContainer_Rsr_Cabinet_S_01",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Rsr Cabinet S 02",
              "technical": "DA_LootContainer_Rsr_Cabinet_S_02",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Rsr Cart Shelf M 01 Drawer 01",
              "technical": "DA_LootContainer_Rsr_CartShelf_M_01_Drawer_01",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Rsr Cart Shelf M 01 Drawer 02",
              "technical": "DA_LootContainer_Rsr_CartShelf_M_01_Drawer_02",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Rsr Cart Shelf M 01 Drawer 05",
              "technical": "DA_LootContainer_Rsr_CartShelf_M_01_Drawer_05",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Rsr Cart Shelf M 01 Drawer 06",
              "technical": "DA_LootContainer_Rsr_CartShelf_M_01_Drawer_06",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Console 03 A",
              "technical": "DA_LootContainer_Console_03_A",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Rsr Electrical Cabinet 01 A",
              "technical": "DA_LootContainer_Rsr_ElectricalCabinet_01_A",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Fax Machine 01 A",
              "technical": "DA_LootContainer_FaxMachine_01_A",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Rsr Lockers 01",
              "technical": "DA_LootContainer_Rsr_Lockers_01",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Lockers 02 A",
              "technical": "DA_LootContainer_Lockers_02_A",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Lockers 02 B",
              "technical": "DA_LootContainer_Lockers_02_B",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Lockers 03",
              "technical": "DA_LootContainer_Lockers_03",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Rsr Machine M 01 A",
              "technical": "DA_LootContainer_Rsr_Machine_M_01_A",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Desk Monitor 01 A",
              "technical": "DA_LootContainer_DeskMonitor_01_A",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Switch Gear Panel 01 Door Breach A",
              "technical": "DA_LootContainer_SwitchGearPanel_01_DoorBreach_A",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Switch Gear Panel 01 Extend Lid A",
              "technical": "DA_LootContainer_SwitchGearPanel_01_ExtendLid_A",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Switch Gear Panel 01 Upper Lid A",
              "technical": "DA_LootContainer_SwitchGearPanel_01_UpperLid_A",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Trashcan 01",
              "technical": "DA_LootContainer_Trashcan_01",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Wrh Wall Add L 01 A Large Panel",
              "technical": "DA_LootContainer_Wrh_WallAdd_L_01_A_LargePanel",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Wrh Wall Add L 01 A Main Panel",
              "technical": "DA_LootContainer_Wrh_WallAdd_L_01_A_MainPanel",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Wrh Wall Add L 01 A Small Panel",
              "technical": "DA_LootContainer_Wrh_WallAdd_L_01_A_SmallPanel",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Rsr Weapons Rack 01 Door Left A",
              "technical": "DA_LootContainer_Rsr_WeaponsRack_01_DoorLeft_A",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Rsr Weapons Rack 01 Door Right A",
              "technical": "DA_LootContainer_Rsr_WeaponsRack_01_DoorRight_A",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Workbench 01 A",
              "technical": "DA_LootContainer_Workbench_01_A",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Workbench 02 A",
              "technical": "DA_LootContainer_Workbench_02_A",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Alpine Wardrobe Door 01",
              "technical": "DA_LootContainer_AlpineWardrobeDoor_01",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Res Closet 01 A Lid 01 A",
              "technical": "DA_LootContainer_Res_Closet_01_A_Lid_01_A",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Res Closet 02 Lid 01 A",
              "technical": "DA_LootContainer_Res_Closet_02_Lid_01_A",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Res Closet 02 Lid 02 A",
              "technical": "DA_LootContainer_Res_Closet_02_Lid_02_A",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Console Chest Drawer 01",
              "technical": "DA_LootContainer_ConsoleChestDrawer_01",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Res Drawer 01 Shelf A",
              "technical": "DA_LootContainer_Res_Drawer_01_Shelf_A",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Res Drawer Medical 01 A",
              "technical": "DA_LootContainer_Res_DrawerMedical_01_A",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Res Dumpster Lid 01",
              "technical": "DA_LootContainer_Res_Dumpster_Lid_01",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Res Bench Kitchen 01 175x125 D",
              "technical": "DA_LootContainer_Res_BenchKitchen_01_175x125_D",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Res Bench Kitchen 01 175x125 Lid 01 C",
              "technical": "DA_LootContainer_Res_BenchKitchen_01_175x125_Lid_01_C",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Res Bench Kitchen 01 Lid 01 175x125 C",
              "technical": "DA_LootContainer_Res_BenchKitchen_01_Lid_01_175x125_C",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Res Bench Kitchen 175x125 Lid 01 A",
              "technical": "DA_LootContainer_Res_BenchKitchen_175x125_Lid_01_A",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Res Bench Kitchen 175x125 Lid 01 B",
              "technical": "DA_LootContainer_Res_BenchKitchen_175x125_Lid_01_B",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Res Refrigerator 01 Lid 01 A",
              "technical": "DA_LootContainer_Res_Refrigerator_01_Lid_01_A",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "SM Res Cupboard Kitchen Lid 01 175x100 A",
              "technical": "DA_LootContainer_SM_Res_CupboardKitchen_Lid_01_175x100_A",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "SM Res Cupboard Kitchen Lid 01 175x100 B",
              "technical": "DA_LootContainer_SM_Res_CupboardKitchen_Lid_01_175x100_B",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "SM Res Cupboard Kitchen Lid 01 300x100 A",
              "technical": "DA_LootContainer_SM_Res_CupboardKitchen_Lid_01_300x100_A",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "SM Res Cupboard Kitchen Lid 01 300x100 B",
              "technical": "DA_LootContainer_SM_Res_CupboardKitchen_Lid_01_300x100_B",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Res Suitcase 01 Lid A",
              "technical": "DA_LootContainer_Res_Suitcase_01_Lid_A",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Res Trash Bin 01 Lid",
              "technical": "DA_LootContainer_Res_TrashBin_01_Lid",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Res Tv Unit 01 A Lid 01",
              "technical": "DA_LootContainer_Res_TvUnit_01_A_Lid_01",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Venetian Drawer 01 A",
              "technical": "DA_LootContainer_VenetianDrawer_01_A",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Sct Crate 01 A",
              "technical": "DA_LootContainer_Sct_Crate_01_A",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Sct Locker 01 A",
              "technical": "DA_LootContainer_Sct_Locker_01_A",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Sct Locker Medical 01 A",
              "technical": "DA_LootContainer_Sct_LockerMedical_01_A",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Truck Scanner 01 Lid A",
              "technical": "DA_LootContainer_TruckScanner_01_Lid_A",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Truck Scanner 01 Lid B",
              "technical": "DA_LootContainer_TruckScanner_01_Lid_B",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Seed Box 01 A",
              "technical": "DA_LootContainer_SeedBox_01_A",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Seed Console 01 A",
              "technical": "DA_LootContainer_SeedConsole_01_A",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Exodus Train Signals Addon Lid 02 A",
              "technical": "DA_LootContainer_ExodusTrain_Signals_Addon_Lid_02_A",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Wrh Computer 01 A",
              "technical": "DA_LootContainer_Wrh_Computer_01_A",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Wrh Container L 02 B",
              "technical": "DA_LootContainer_Wrh_Container_L_02_B",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Wrh Desk Drawer Large",
              "technical": "DA_LootContainer_Wrh_Desk_DrawerLarge",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Wrh Desk Drawer Small",
              "technical": "DA_LootContainer_Wrh_Desk_DrawerSmall",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Wrh Drawers 01 A",
              "technical": "DA_LootContainer_Wrh_Drawers_01_A",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Wrh Machine M 02 Door Left",
              "technical": "DA_LootContainer_Wrh_Machine_M_02_DoorLeft",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Wrh Machine M 02 Door Right",
              "technical": "DA_LootContainer_Wrh_Machine_M_02_DoorRight",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Wrh Trailer Compressor M 01 A",
              "technical": "DA_LootContainer_Wrh_TrailerCompressor_M_01_A",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Wrh Trolley Tools 01 A",
              "technical": "DA_LootContainer_Wrh_TrolleyTools_01_A",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Wrh Wall Add M 02 A",
              "technical": "DA_LootContainer_Wrh_WallAdd_M_02_A",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Wrh Wall Add S 01 A Panel Large",
              "technical": "DA_LootContainer_Wrh_WallAdd_S_01_A_PanelLarge",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Wrh Wall Add S 01 A Panel Small",
              "technical": "DA_LootContainer_Wrh_WallAdd_S_01_A_PanelSmall",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Moss Rock",
              "technical": "DA_LootContainer_Moss_Rock",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Agave",
              "technical": "DA_LootContainer_Agave",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Resin",
              "technical": "DA_LootContainer_Resin",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Mushroom Cluster 01 A",
              "technical": "DA_LootContainer_MushroomCluster_01_A",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            },
            {
              "name": "Volcanic Rock Temp",
              "technical": "DA_LootContainer_VolcanicRock_Temp",
              "status": "pending",
              "note": "Loot container identified; population, searching and item transfer are not implemented."
            }
          ]
        },
        {
          "name": "Extraction · map unresolved",
          "children": [
            {
              "name": "Collapsed Store",
              "technical": "DA_ExtractionPoint_CollapsedStore",
              "status": "pending",
              "note": "Map binding is unresolved; do not infer a map from a generic name."
            },
            {
              "name": "Control",
              "technical": "DA_ExtractionPoint_Control",
              "status": "pending",
              "note": "Map binding is unresolved; do not infer a map from a generic name."
            },
            {
              "name": "Departure",
              "technical": "DA_ExtractionPoint_Departure",
              "status": "pending",
              "note": "Map binding is unresolved; do not infer a map from a generic name."
            },
            {
              "name": "FZT River",
              "technical": "DA_ExtractionPoint_FZT_River",
              "status": "pending",
              "note": "Map binding is unresolved; do not infer a map from a generic name."
            },
            {
              "name": "FZT Supermarket",
              "technical": "DA_ExtractionPoint_FZT_Supermarket",
              "status": "pending",
              "note": "Map binding is unresolved; do not infer a map from a generic name."
            },
            {
              "name": "FZT Tracks",
              "technical": "DA_ExtractionPoint_FZT_Tracks",
              "status": "pending",
              "note": "Map binding is unresolved; do not infer a map from a generic name."
            },
            {
              "name": "FZT Village",
              "technical": "DA_ExtractionPoint_FZT_Village",
              "status": "pending",
              "note": "Map binding is unresolved; do not infer a map from a generic name."
            },
            {
              "name": "Generic",
              "technical": "DA_ExtractionPoint_Generic",
              "status": "pending",
              "note": "Map binding is unresolved; do not infer a map from a generic name."
            },
            {
              "name": "Highway Overpass",
              "technical": "DA_ExtractionPoint_HighwayOverpass",
              "status": "pending",
              "note": "Map binding is unresolved; do not infer a map from a generic name."
            },
            {
              "name": "North Complex",
              "technical": "DA_ExtractionPoint_NorthComplex",
              "status": "partial",
              "note": "Dam elevator: calling it, the descend hold and leaving to the main menu work; the loadout and carried items come home."
            },
            {
              "name": "Old Baron",
              "technical": "DA_ExtractionPoint_OldBaron",
              "status": "pending",
              "note": "Map binding is unresolved; do not infer a map from a generic name."
            },
            {
              "name": "Old Town",
              "technical": "DA_ExtractionPoint_OldTown",
              "status": "pending",
              "note": "Map binding is unresolved; do not infer a map from a generic name."
            },
            {
              "name": "Onboarding Level",
              "technical": "DA_ExtractionPoint_OnboardingLevel",
              "status": "pending",
              "note": "Map binding is unresolved; do not infer a map from a generic name."
            },
            {
              "name": "Practice Range",
              "technical": "DA_ExtractionPoint_PracticeRange",
              "status": "pending",
              "note": "Map binding is unresolved; do not infer a map from a generic name."
            },
            {
              "name": "Pump House",
              "technical": "DA_ExtractionPoint_PumpHouse",
              "status": "pending",
              "note": "Map binding is unresolved; do not infer a map from a generic name."
            },
            {
              "name": "Red Lake Balcony",
              "technical": "DA_ExtractionPoint_RedLakeBalcony",
              "status": "partial",
              "note": "Dam elevator: calling it, the descend hold and leaving to the main menu work; the loadout and carried items come home."
            },
            {
              "name": "Spillway",
              "technical": "DA_ExtractionPoint_Spillway",
              "status": "pending",
              "note": "Map binding is unresolved; do not infer a map from a generic name."
            },
            {
              "name": "Sunroof",
              "technical": "DA_ExtractionPoint_Sunroof",
              "status": "pending",
              "note": "Map binding is unresolved; do not infer a map from a generic name."
            },
            {
              "name": "Swamp Lift",
              "technical": "DA_ExtractionPoint_SwampLift",
              "status": "partial",
              "note": "Dam elevator: calling it, the descend hold and leaving to the main menu work; the loadout and carried items come home."
            },
            {
              "name": "Swampside",
              "technical": "DA_ExtractionPoint_Swampside",
              "status": "partial",
              "note": "Dam elevator: calling it, the descend hold and leaving to the main menu work; the loadout and carried items come home."
            },
            {
              "name": "TBG Airshaft Central",
              "technical": "DA_ExtractionPoint_TBG_AirshaftCentral",
              "status": "pending",
              "note": "Map binding is unresolved; do not infer a map from a generic name."
            },
            {
              "name": "TBG Airshaft East",
              "technical": "DA_ExtractionPoint_TBG_AirshaftEast",
              "status": "pending",
              "note": "Map binding is unresolved; do not infer a map from a generic name."
            },
            {
              "name": "TBG Airshaft North",
              "technical": "DA_ExtractionPoint_TBG_AirshaftNorth",
              "status": "pending",
              "note": "Map binding is unresolved; do not infer a map from a generic name."
            },
            {
              "name": "TBG Airshaft South",
              "technical": "DA_ExtractionPoint_TBG_AirshaftSouth",
              "status": "pending",
              "note": "Map binding is unresolved; do not infer a map from a generic name."
            },
            {
              "name": "TBG Hatch Forest",
              "technical": "DA_ExtractionPoint_TBG_HatchForest",
              "status": "pending",
              "note": "Map binding is unresolved; do not infer a map from a generic name."
            },
            {
              "name": "TBG Hatch Mountains",
              "technical": "DA_ExtractionPoint_TBG_HatchMountains",
              "status": "pending",
              "note": "Map binding is unresolved; do not infer a map from a generic name."
            },
            {
              "name": "TBG Hatch South",
              "technical": "DA_ExtractionPoint_TBG_HatchSouth",
              "status": "pending",
              "note": "Map binding is unresolved; do not infer a map from a generic name."
            },
            {
              "name": "TBG Hatch Village",
              "technical": "DA_ExtractionPoint_TBG_HatchVillage",
              "status": "pending",
              "note": "Map binding is unresolved; do not infer a map from a generic name."
            },
            {
              "name": "Train Station",
              "technical": "DA_ExtractionPoint_TrainStation",
              "status": "pending",
              "note": "Map binding is unresolved; do not infer a map from a generic name."
            },
            {
              "name": "Trench East",
              "technical": "DA_ExtractionPoint_TrenchEast",
              "status": "pending",
              "note": "Map binding is unresolved; do not infer a map from a generic name."
            },
            {
              "name": "Trench West",
              "technical": "DA_ExtractionPoint_TrenchWest",
              "status": "pending",
              "note": "Map binding is unresolved; do not infer a map from a generic name."
            }
          ]
        }
      ]
    },
    {
      "name": "World objectives",
      "children": [
        {
          "name": "Any Map",
          "technical": "DA_QuestArea_AnyMap",
          "status": "unassessed"
        },
        {
          "name": "AWarm Place To Rest Grave",
          "technical": "DA_QuestArea_AWarmPlaceToRest_Grave",
          "status": "unassessed"
        },
        {
          "name": "BG Olive Grove",
          "technical": "DA_QuestArea_BG_OliveGrove",
          "status": "unassessed"
        },
        {
          "name": "Bilguns Raider Camp",
          "technical": "DA_QuestArea_Bilguns_RaiderCamp",
          "status": "unassessed"
        },
        {
          "name": "Blue Gate",
          "technical": "DA_QuestArea_BlueGate",
          "status": "unassessed"
        },
        {
          "name": "Blue Gate The Clean Dream",
          "technical": "DA_QuestArea_BlueGate_TheCleanDream",
          "status": "unassessed"
        },
        {
          "name": "Broken Monument Hallowed Grounds",
          "technical": "DA_QuestArea_BrokenMonument_HallowedGrounds",
          "status": "unassessed"
        },
        {
          "name": "Buried City",
          "technical": "DA_QuestArea_BuriedCity",
          "status": "unassessed"
        },
        {
          "name": "Buried City Apartment Building B2",
          "technical": "DA_QuestArea_BuriedCity_ApartmentBuildingB2",
          "status": "unassessed"
        },
        {
          "name": "Buried City Breachable Parking Garage",
          "technical": "DA_QuestArea_BuriedCity_BreachableParkingGarage",
          "status": "unassessed"
        },
        {
          "name": "Buried City Breachable Space Travel",
          "technical": "DA_QuestArea_BuriedCity_BreachableSpaceTravel",
          "status": "unassessed"
        },
        {
          "name": "Buried City Digging Up Dirt",
          "technical": "DA_QuestArea_BuriedCity_DiggingUpDirt",
          "status": "unassessed"
        },
        {
          "name": "Buried City Flooded Sun Cells",
          "technical": "DA_QuestArea_BuriedCity_FloodedSunCells",
          "status": "unassessed"
        },
        {
          "name": "Buried City Galleria",
          "technical": "DA_QuestArea_BuriedCity_Galleria",
          "status": "unassessed"
        },
        {
          "name": "Buried City Gas Station",
          "technical": "DA_QuestArea_BuriedCity_GasStation",
          "status": "unassessed"
        },
        {
          "name": "Buried City Grandioso Apartments",
          "technical": "DA_QuestArea_BuriedCity_GrandiosoApartments",
          "status": "unassessed"
        },
        {
          "name": "Buried City Grandioso Apartments Locked Room",
          "technical": "DA_QuestArea_BuriedCity_GrandiosoApartments_LockedRoom",
          "status": "unassessed"
        },
        {
          "name": "Buried City Hospital",
          "technical": "DA_QuestArea_BuriedCity_Hospital",
          "status": "unassessed"
        },
        {
          "name": "Buried City Hospital Locked Room",
          "technical": "DA_QuestArea_BuriedCity_Hospital_LockedRoom",
          "status": "unassessed"
        },
        {
          "name": "Buried City Library",
          "technical": "DA_QuestArea_BuriedCity_Library",
          "status": "unassessed"
        },
        {
          "name": "Buried City Main Street Locked Room",
          "technical": "DA_QuestArea_BuriedCity_MainStreet_LockedRoom",
          "status": "unassessed"
        },
        {
          "name": "Buried City Marked For Death",
          "technical": "DA_QuestArea_BuriedCity_MarkedForDeath",
          "status": "unassessed"
        },
        {
          "name": "Buried City Market Correction",
          "technical": "DA_QuestArea_BuriedCity_MarketCorrection",
          "status": "unassessed"
        },
        {
          "name": "Buried City Mutiny Block Locked Room",
          "technical": "DA_QuestArea_BuriedCity_MutinyBlock_LockedRoom",
          "status": "unassessed"
        },
        {
          "name": "Buried City Old Town",
          "technical": "DA_QuestArea_BuriedCity_OldTown",
          "status": "unassessed"
        },
        {
          "name": "Buried City Piazza Arbusto Pharmacy",
          "technical": "DA_QuestArea_BuriedCity_PiazzaArbusto_Pharmacy",
          "status": "unassessed"
        },
        {
          "name": "Buried City Piazza Roma",
          "technical": "DA_QuestArea_BuriedCity_PiazzaRoma",
          "status": "unassessed"
        },
        {
          "name": "Buried City Plaza Rosa",
          "technical": "DA_QuestArea_BuriedCity_PlazaRosa",
          "status": "unassessed"
        },
        {
          "name": "Buried City Plaza Rosa Farmacia",
          "technical": "DA_QuestArea_BuriedCity_PlazaRosa_Farmacia",
          "status": "unassessed"
        },
        {
          "name": "Buried City Red Tower",
          "technical": "DA_QuestArea_BuriedCity_RedTower",
          "status": "unassessed"
        },
        {
          "name": "Buried City Soaking Up The Sun",
          "technical": "DA_QuestArea_BuriedCity_SoakingUpTheSun",
          "status": "unassessed"
        },
        {
          "name": "Buried City Solar Tampering",
          "technical": "DA_QuestArea_BuriedCity_SolarTampering",
          "status": "unassessed"
        },
        {
          "name": "Buried City Space Travel Locked Room",
          "technical": "DA_QuestArea_BuriedCity_SpaceTravel_LockedRoom",
          "status": "unassessed"
        },
        {
          "name": "Buried City Town Hall",
          "technical": "DA_QuestArea_BuriedCity_TownHall",
          "status": "unassessed"
        },
        {
          "name": "Buried City West Village",
          "technical": "DA_QuestArea_BuriedCity_WestVillage",
          "status": "unassessed"
        },
        {
          "name": "Buried City WV Apt Building A1",
          "technical": "DA_QuestArea_BuriedCity_WV_AptBuildingA1",
          "status": "unassessed"
        },
        {
          "name": "Buried City WV Apt Building A2",
          "technical": "DA_QuestArea_BuriedCity_WV_AptBuildingA2",
          "status": "unassessed"
        },
        {
          "name": "Buried City WV Trattoria Piccolo",
          "technical": "DA_QuestArea_BuriedCity_WV_TrattoriaPiccolo",
          "status": "unassessed"
        },
        {
          "name": "Formicia Outpost",
          "technical": "DA_QuestArea_FormiciaOutpost",
          "status": "unassessed"
        },
        {
          "name": "Frozen Trails Supermarket Interior",
          "technical": "DA_QuestArea_FrozenTrails_SupermarketInterior",
          "status": "unassessed"
        },
        {
          "name": "Harvester",
          "technical": "DA_QuestArea_Harvester",
          "status": "unassessed"
        },
        {
          "name": "Highlighted Loot Area",
          "technical": "DA_QuestArea_HighlightedLootArea",
          "status": "unassessed"
        },
        {
          "name": "Offer Refuse Caches",
          "technical": "DA_QuestArea_OfferRefuse_Caches",
          "status": "unassessed"
        },
        {
          "name": "Offer Refuse Fort",
          "technical": "DA_QuestArea_OfferRefuse_Fort",
          "status": "unassessed"
        },
        {
          "name": "POI Alcantara",
          "technical": "DA_QuestArea_POI_Alcantara",
          "status": "unassessed"
        },
        {
          "name": "POI BG Abandoned Housing Project",
          "technical": "DA_QuestArea_POI_BG_AbandonedHousingProject",
          "status": "unassessed"
        },
        {
          "name": "POI BG ARCDeforestation Zone",
          "technical": "DA_QuestArea_POI_BG_ARCDeforestationZone",
          "status": "unassessed"
        },
        {
          "name": "POI BG Broken Earth",
          "technical": "DA_QuestArea_POI_BG_BrokenEarth",
          "status": "unassessed"
        },
        {
          "name": "POI BG Broken Earth Deforester",
          "technical": "DA_QuestArea_POI_BG_BrokenEarth_Deforester",
          "status": "unassessed"
        },
        {
          "name": "POI BG Checkpoint",
          "technical": "DA_QuestArea_POI_BG_Checkpoint",
          "status": "unassessed"
        },
        {
          "name": "POI BG Communication Tower",
          "technical": "DA_QuestArea_POI_BG_CommunicationTower",
          "status": "unassessed"
        },
        {
          "name": "POI BG Farmlands",
          "technical": "DA_QuestArea_POI_BG_Farmlands",
          "status": "unassessed"
        },
        {
          "name": "POI BG Gate Proper",
          "technical": "DA_QuestArea_POI_BG_GateProper",
          "status": "unassessed"
        },
        {
          "name": "POI BG Highway Collapse",
          "technical": "DA_QuestArea_POI_BG_HighwayCollapse",
          "status": "unassessed"
        },
        {
          "name": "POI BG Loading Dock",
          "technical": "DA_QuestArea_POI_BG_LoadingDock",
          "status": "unassessed"
        },
        {
          "name": "POI BG Logistics Office",
          "technical": "DA_QuestArea_POI_BG_LogisticsOffice",
          "status": "unassessed"
        },
        {
          "name": "POI BG Mantikor Site Entrance",
          "technical": "DA_QuestArea_POI_BG_MantikorSiteEntrance",
          "status": "unassessed"
        },
        {
          "name": "POI BG Modified Wreck",
          "technical": "DA_QuestArea_POI_BG_ModifiedWreck",
          "status": "unassessed"
        },
        {
          "name": "POI BG Raiders Refuge",
          "technical": "DA_QuestArea_POI_BG_RaidersRefuge",
          "status": "unassessed"
        },
        {
          "name": "POI BG Research Center",
          "technical": "DA_QuestArea_POI_BG_ResearchCenter",
          "status": "unassessed"
        },
        {
          "name": "POI BG Ridgeline",
          "technical": "DA_QuestArea_POI_BG_Ridgeline",
          "status": "unassessed"
        },
        {
          "name": "POI BG Station Office",
          "technical": "DA_QuestArea_POI_BG_StationOffice",
          "status": "unassessed"
        },
        {
          "name": "POI BG The Forest",
          "technical": "DA_QuestArea_POI_BG_TheForest",
          "status": "unassessed"
        },
        {
          "name": "POI BG The Mountains",
          "technical": "DA_QuestArea_POI_BG_TheMountains",
          "status": "unassessed"
        },
        {
          "name": "POI BG Traffic Tunnel",
          "technical": "DA_QuestArea_POI_BG_TrafficTunnel",
          "status": "unassessed"
        },
        {
          "name": "POI BG Warehouse Complex",
          "technical": "DA_QuestArea_POI_BG_WarehouseComplex",
          "status": "unassessed"
        },
        {
          "name": "POI Buried City Abandoned Highway Camp",
          "technical": "DA_QuestArea_POI_BuriedCity_AbandonedHighwayCamp",
          "status": "unassessed"
        },
        {
          "name": "POI Buried City Research Building",
          "technical": "DA_QuestArea_POI_BuriedCity_ResearchBuilding",
          "status": "unassessed"
        },
        {
          "name": "POI Buried City Research Lab",
          "technical": "DA_QuestArea_POI_BuriedCity_ResearchLab",
          "status": "unassessed"
        },
        {
          "name": "POI Practice Range",
          "technical": "DA_QuestArea_POI_PracticeRange",
          "status": "unassessed"
        },
        {
          "name": "POI Spaceport",
          "technical": "DA_QuestArea_POI_Spaceport",
          "status": "unassessed"
        },
        {
          "name": "POI Spaceport Admin Building",
          "technical": "DA_QuestArea_POI_Spaceport_AdminBuilding",
          "status": "unassessed"
        },
        {
          "name": "POI Spaceport Admin Building Lockers",
          "technical": "DA_QuestArea_POI_Spaceport_AdminBuilding_Lockers",
          "status": "unassessed"
        },
        {
          "name": "POI Spaceport Admin Building Medical Exam Room",
          "technical": "DA_QuestArea_POI_Spaceport_AdminBuilding_MedicalExamRoom",
          "status": "unassessed"
        },
        {
          "name": "POI Spaceport Admin Building Security",
          "technical": "DA_QuestArea_POI_Spaceport_AdminBuilding_Security",
          "status": "unassessed"
        },
        {
          "name": "POI Spaceport Arrival Building",
          "technical": "DA_QuestArea_POI_Spaceport_ArrivalBuilding",
          "status": "unassessed"
        },
        {
          "name": "POI Spaceport Arrival Building Data Storage",
          "technical": "DA_QuestArea_POI_Spaceport_ArrivalBuilding_DataStorage",
          "status": "unassessed"
        },
        {
          "name": "POI Spaceport Broken Pipeline",
          "technical": "DA_QuestArea_POI_Spaceport_BrokenPipeline",
          "status": "unassessed"
        },
        {
          "name": "POI Spaceport Colony Field Depot",
          "technical": "DA_QuestArea_POI_Spaceport_ColonyFieldDepot",
          "status": "unassessed"
        },
        {
          "name": "POI Spaceport Comm Tower",
          "technical": "DA_QuestArea_POI_Spaceport_CommTower",
          "status": "unassessed"
        },
        {
          "name": "POI Spaceport Container Storage Locked Room",
          "technical": "DA_QuestArea_POI_Spaceport_ContainerStorageLockedRoom",
          "status": "unassessed"
        },
        {
          "name": "POI Spaceport Container Storage Room",
          "technical": "DA_QuestArea_POI_Spaceport_ContainerStorageRoom",
          "status": "unassessed"
        },
        {
          "name": "POI Spaceport Container Yard",
          "technical": "DA_QuestArea_POI_Spaceport_ContainerYard",
          "status": "unassessed"
        },
        {
          "name": "POI Spaceport Control Tower",
          "technical": "DA_QuestArea_POI_Spaceport_ControlTower",
          "status": "unassessed"
        },
        {
          "name": "POI Spaceport Control Tower Top",
          "technical": "DA_QuestArea_POI_Spaceport_ControlTower_Top",
          "status": "unassessed"
        },
        {
          "name": "POI Spaceport Control Tower Top",
          "technical": "DA_QuestArea_POI_Spaceport_ControlTowerTop",
          "status": "unassessed"
        },
        {
          "name": "POI Spaceport East Plains",
          "technical": "DA_QuestArea_POI_Spaceport_EastPlains",
          "status": "unassessed"
        },
        {
          "name": "POI Spaceport Electrical Substation",
          "technical": "DA_QuestArea_POI_Spaceport_ElectricalSubstation",
          "status": "unassessed"
        },
        {
          "name": "POI Spaceport Encampment Wasabi",
          "technical": "DA_QuestArea_POI_Spaceport_EncampmentWasabi",
          "status": "unassessed"
        },
        {
          "name": "POI Spaceport Encampment Wasabi Baron Husk",
          "technical": "DA_QuestArea_POI_Spaceport_EncampmentWasabi_BaronHusk",
          "status": "unassessed"
        },
        {
          "name": "POI Spaceport Facility Support",
          "technical": "DA_QuestArea_POI_Spaceport_FacilitySupport",
          "status": "unassessed"
        },
        {
          "name": "POI Spaceport Facility Support Container Storage",
          "technical": "DA_QuestArea_POI_Spaceport_FacilitySupport_ContainerStorage",
          "status": "unassessed"
        },
        {
          "name": "POI Spaceport Facility Support Vehicle Maintenance",
          "technical": "DA_QuestArea_POI_Spaceport_FacilitySupport_VehicleMaintenance",
          "status": "unassessed"
        },
        {
          "name": "POI Spaceport Facility Support Vehicle Maintenance Control Room",
          "technical": "DA_QuestArea_POI_Spaceport_FacilitySupport_VehicleMaintenance_ControlRoom",
          "status": "unassessed"
        },
        {
          "name": "POI Spaceport Food Court",
          "technical": "DA_QuestArea_POI_Spaceport_FoodCourt",
          "status": "unassessed"
        },
        {
          "name": "POI Spaceport Fuel Pipeline Baron Husk",
          "technical": "DA_QuestArea_POI_Spaceport_FuelPipeline_BaronHusk",
          "status": "unassessed"
        },
        {
          "name": "POI Spaceport Gas Station",
          "technical": "DA_QuestArea_POI_Spaceport_GasStation",
          "status": "unassessed"
        },
        {
          "name": "POI Spaceport Hatch",
          "technical": "DA_QuestArea_POI_Spaceport_Hatch",
          "status": "unassessed"
        },
        {
          "name": "POI Spaceport Hatch Control",
          "technical": "DA_QuestArea_POI_Spaceport_HatchControl",
          "status": "unassessed"
        },
        {
          "name": "POI Spaceport Hatch Trench East",
          "technical": "DA_QuestArea_POI_Spaceport_HatchTrenchEast",
          "status": "unassessed"
        },
        {
          "name": "POI Spaceport Hatch Trench West",
          "technical": "DA_QuestArea_POI_Spaceport_HatchTrenchWest",
          "status": "unassessed"
        },
        {
          "name": "POI Spaceport Hatch Tunnels",
          "technical": "DA_QuestArea_POI_Spaceport_HatchTunnels",
          "status": "unassessed"
        },
        {
          "name": "POI Spaceport Inside Walls",
          "technical": "DA_QuestArea_POI_Spaceport_InsideWalls",
          "status": "unassessed"
        },
        {
          "name": "POI Spaceport La Chiesa",
          "technical": "DA_QuestArea_POI_Spaceport_LaChiesa",
          "status": "unassessed"
        },
        {
          "name": "POI Spaceport Launch Tower Launch Pad",
          "technical": "DA_QuestArea_POI_Spaceport_LaunchTower_LaunchPad",
          "status": "unassessed"
        },
        {
          "name": "POI Spaceport Launch Towers",
          "technical": "DA_QuestArea_POI_Spaceport_LaunchTowers",
          "status": "unassessed"
        },
        {
          "name": "POI Spaceport Launch Towers East",
          "technical": "DA_QuestArea_POI_Spaceport_LaunchTowersEast",
          "status": "unassessed"
        },
        {
          "name": "POI Spaceport Launch Towers West",
          "technical": "DA_QuestArea_POI_Spaceport_LaunchTowersWest",
          "status": "unassessed"
        },
        {
          "name": "POI Spaceport Medical Bay",
          "technical": "DA_QuestArea_POI_Spaceport_MedicalBay",
          "status": "unassessed"
        },
        {
          "name": "POI Spaceport Outside Walls",
          "technical": "DA_QuestArea_POI_Spaceport_OutsideWalls",
          "status": "unassessed"
        },
        {
          "name": "POI Spaceport Security Checkpoints",
          "technical": "DA_QuestArea_POI_Spaceport_SecurityCheckpoints",
          "status": "unassessed"
        },
        {
          "name": "POI Spaceport The Spaceport",
          "technical": "DA_QuestArea_POI_Spaceport_TheSpaceport",
          "status": "unassessed"
        },
        {
          "name": "POI Spaceport Transit Bunker",
          "technical": "DA_QuestArea_POI_Spaceport_TransitBunker",
          "status": "unassessed"
        },
        {
          "name": "POI Spaceport Transit Bunker West",
          "technical": "DA_QuestArea_POI_Spaceport_TransitBunker_West",
          "status": "unassessed"
        },
        {
          "name": "POI Spaceport Transit Bunker South",
          "technical": "DA_QuestArea_POI_Spaceport_TransitBunkerSouth",
          "status": "unassessed"
        },
        {
          "name": "POI Spaceport Trench",
          "technical": "DA_QuestArea_POI_Spaceport_Trench",
          "status": "unassessed"
        },
        {
          "name": "POI Spaceport Tunnels",
          "technical": "DA_QuestArea_POI_Spaceport_Tunnels",
          "status": "unassessed"
        },
        {
          "name": "POI Spaceport Wasabi Wreckage",
          "technical": "DA_QuestArea_POI_Spaceport_WasabiWreckage",
          "status": "unassessed"
        },
        {
          "name": "POI Spaceport Water Towers",
          "technical": "DA_QuestArea_POI_Spaceport_WaterTowers",
          "status": "unassessed"
        },
        {
          "name": "POI The Dam Admin",
          "technical": "DA_QuestArea_POI_TheDam_Admin",
          "status": "unassessed"
        },
        {
          "name": "POI The Dam Admin Staff Room",
          "technical": "DA_QuestArea_POI_TheDam_Admin_StaffRoom",
          "status": "unassessed"
        },
        {
          "name": "POI The Dam Administration Reception",
          "technical": "DA_QuestArea_POI_TheDam_AdministrationReception",
          "status": "unassessed"
        },
        {
          "name": "POI The Dam Control Tower Top",
          "technical": "DA_QuestArea_POI_TheDam_ControlTowerTop",
          "status": "unassessed"
        },
        {
          "name": "POI The Dam Deployment Tunnel Antenna",
          "technical": "DA_QuestArea_POI_TheDam_DeploymentTunnelAntenna",
          "status": "unassessed"
        },
        {
          "name": "POI The Dam Deployment Tunnel Bunker",
          "technical": "DA_QuestArea_POI_TheDam_DeploymentTunnelBunker",
          "status": "unassessed"
        },
        {
          "name": "POI The Dam Deployment Tunnel Dam",
          "technical": "DA_QuestArea_POI_TheDam_DeploymentTunnelDam",
          "status": "unassessed"
        },
        {
          "name": "POI The Dam Drainage Tunnel",
          "technical": "DA_QuestArea_POI_TheDam_DrainageTunnel",
          "status": "unassessed"
        },
        {
          "name": "POI The Dam Electrical Substation",
          "technical": "DA_QuestArea_POI_TheDam_ElectricalSubstation",
          "status": "unassessed"
        },
        {
          "name": "POI The Dam Flood Access Tunnel",
          "technical": "DA_QuestArea_POI_TheDam_FloodAccessTunnel",
          "status": "unassessed"
        },
        {
          "name": "POI The Dam Flood Access Tunnel Water",
          "technical": "DA_QuestArea_POI_TheDam_FloodAccessTunnel_Water",
          "status": "unassessed"
        },
        {
          "name": "POI The Dam Generator Complex",
          "technical": "DA_QuestArea_POI_TheDam_GeneratorComplex",
          "status": "unassessed"
        },
        {
          "name": "POI The Dam Hatch Bens Sunroof",
          "technical": "DA_QuestArea_POI_TheDam_HatchBensSunroof",
          "status": "unassessed"
        },
        {
          "name": "POI The Dam Hatch Pumphouse",
          "technical": "DA_QuestArea_POI_TheDam_HatchPumphouse",
          "status": "unassessed"
        },
        {
          "name": "POI The Dam Hatch Pumphouse Barons",
          "technical": "DA_QuestArea_POI_TheDam_HatchPumphouseBarons",
          "status": "unassessed"
        },
        {
          "name": "POI The Dam Hatch Spillway",
          "technical": "DA_QuestArea_POI_TheDam_HatchSpillway",
          "status": "unassessed"
        },
        {
          "name": "POI The Dam Hydroponic Dome",
          "technical": "DA_QuestArea_POI_TheDam_HydroponicDome",
          "status": "unassessed"
        },
        {
          "name": "POI The Dam Hydroponic Dome Complex",
          "technical": "DA_QuestArea_POI_TheDam_HydroponicDomeComplex",
          "status": "unassessed"
        },
        {
          "name": "POI The Dam Loading Bay",
          "technical": "DA_QuestArea_POI_TheDam_LoadingBay",
          "status": "unassessed"
        },
        {
          "name": "POI The Dam Maintenance",
          "technical": "DA_QuestArea_POI_TheDam_Maintenance",
          "status": "unassessed"
        },
        {
          "name": "POI The Dam Old Battleground",
          "technical": "DA_QuestArea_POI_TheDam_OldBattleground",
          "status": "unassessed"
        },
        {
          "name": "POI The Dam Pattern House",
          "technical": "DA_QuestArea_POI_TheDam_PatternHouse",
          "status": "unassessed"
        },
        {
          "name": "POI The Dam Primary Facilities",
          "technical": "DA_QuestArea_POI_TheDam_PrimaryFacilities",
          "status": "unassessed"
        },
        {
          "name": "POI The Dam Raider Outpost East",
          "technical": "DA_QuestArea_POI_TheDam_RaiderOutpostEast",
          "status": "unassessed"
        },
        {
          "name": "POI The Dam Red Lake Electrical Tower",
          "technical": "DA_QuestArea_POI_TheDam_RedLake_ElectricalTower",
          "status": "unassessed"
        },
        {
          "name": "POI The Dam Red Lakes",
          "technical": "DA_QuestArea_POI_TheDam_RedLakes",
          "status": "unassessed"
        },
        {
          "name": "POI The Dam Research Lab",
          "technical": "DA_QuestArea_POI_TheDam_ResearchLab",
          "status": "unassessed"
        },
        {
          "name": "POI The Dam Research Station",
          "technical": "DA_QuestArea_POI_TheDam_ResearchStation",
          "status": "unassessed"
        },
        {
          "name": "POI The Dam South Swamp Outpost",
          "technical": "DA_QuestArea_POI_TheDam_SouthSwampOutpost",
          "status": "unassessed"
        },
        {
          "name": "POI The Dam Spillway Gates",
          "technical": "DA_QuestArea_POI_TheDam_SpillwayGates",
          "status": "unassessed"
        },
        {
          "name": "POI The Dam Spilway Gates",
          "technical": "DA_QuestArea_POI_TheDam_SpilwayGates",
          "status": "unassessed"
        },
        {
          "name": "POI The Dam Swamp Flood Spill Intake",
          "technical": "DA_QuestArea_POI_TheDam_Swamp_FloodSpillIntake",
          "status": "unassessed"
        },
        {
          "name": "POI The Dam Swampside Elevator Courtyard",
          "technical": "DA_QuestArea_POI_TheDam_SwampsideElevatorCourtyard",
          "status": "unassessed"
        },
        {
          "name": "POI The Dam Testing Annex",
          "technical": "DA_QuestArea_POI_TheDam_TestingAnnex",
          "status": "unassessed"
        },
        {
          "name": "POI The Dam Testing Annex Flooded Cellar",
          "technical": "DA_QuestArea_POI_TheDam_TestingAnnex_FloodedCellar",
          "status": "unassessed"
        },
        {
          "name": "POI The Dam Testing Annex Information",
          "technical": "DA_QuestArea_POI_TheDam_TestingAnnex_Information",
          "status": "unassessed"
        },
        {
          "name": "POI The Dam Testing Annex Locked Room A",
          "technical": "DA_QuestArea_POI_TheDam_TestingAnnex_LockedRoomA",
          "status": "unassessed"
        },
        {
          "name": "POI The Dam Testing Annex Locked Room B",
          "technical": "DA_QuestArea_POI_TheDam_TestingAnnex_LockedRoomB",
          "status": "unassessed"
        },
        {
          "name": "POI The Dam Testing Annex Monitoringroom1",
          "technical": "DA_QuestArea_POI_TheDam_TestingAnnex_Monitoringroom1",
          "status": "unassessed"
        },
        {
          "name": "POI The Dam The Breach",
          "technical": "DA_QuestArea_POI_TheDam_TheBreach",
          "status": "unassessed"
        },
        {
          "name": "POI The Dam Wasabi Wreckage",
          "technical": "DA_QuestArea_POI_TheDam_WasabiWreckage",
          "status": "unassessed"
        },
        {
          "name": "POI The Dam Water Control Building",
          "technical": "DA_QuestArea_POI_TheDam_WaterControlBuilding",
          "status": "unassessed"
        },
        {
          "name": "POI The Dam Water Control Building Control Room",
          "technical": "DA_QuestArea_POI_TheDam_WaterControlBuilding_ControlRoom",
          "status": "unassessed"
        },
        {
          "name": "POI The Dam Water Control Building Locked Room",
          "technical": "DA_QuestArea_POI_TheDam_WaterControlBuilding_LockedRoom",
          "status": "unassessed"
        },
        {
          "name": "POI The Dam Water Saboteur",
          "technical": "DA_QuestArea_POI_TheDam_WaterSaboteur",
          "status": "unassessed"
        },
        {
          "name": "POI The Dam Water Towers",
          "technical": "DA_QuestArea_POI_TheDam_WaterTowers",
          "status": "unassessed"
        },
        {
          "name": "Raider Camp",
          "technical": "DA_QuestArea_RaiderCamp",
          "status": "unassessed"
        },
        {
          "name": "Raider Memorial Husk",
          "technical": "DA_QuestArea_RaiderMemorial_Husk",
          "status": "unassessed"
        },
        {
          "name": "Riven Tides Beaches",
          "technical": "DA_QuestArea_RivenTides_Beaches",
          "status": "unassessed"
        },
        {
          "name": "Riven Tides Hotel",
          "technical": "DA_QuestArea_RivenTides_Hotel",
          "status": "unassessed"
        },
        {
          "name": "SM Cafeteria",
          "technical": "DA_QuestArea_SM_Cafeteria",
          "status": "unassessed"
        },
        {
          "name": "SM Loading Bay",
          "technical": "DA_QuestArea_SM_LoadingBay",
          "status": "unassessed"
        },
        {
          "name": "Spaceport Bunker Interior",
          "technical": "DA_QuestArea_Spaceport_BunkerInterior",
          "status": "unassessed"
        },
        {
          "name": "Spaceport Departure Building Lobby",
          "technical": "DA_QuestArea_Spaceport_DepartureBuilding_Lobby",
          "status": "unassessed"
        },
        {
          "name": "Spaceport Flight Control Building Upper Floor",
          "technical": "DA_QuestArea_Spaceport_FlightControlBuilding_UpperFloor",
          "status": "unassessed"
        },
        {
          "name": "Spaceport Jiangsu Warehouse",
          "technical": "DA_QuestArea_Spaceport_JiangsuWarehouse",
          "status": "unassessed"
        },
        {
          "name": "Spaceport Jiangsu Warehouse Locked Room",
          "technical": "DA_QuestArea_Spaceport_JiangsuWarehouse_LockedRoom",
          "status": "unassessed"
        },
        {
          "name": "Spaceport Outskirts Bunker",
          "technical": "DA_QuestArea_Spaceport_OutskirtsBunker",
          "status": "unassessed"
        },
        {
          "name": "Spaceport Top Of Launch Towers",
          "technical": "DA_QuestArea_Spaceport_TopOfLaunchTowers",
          "status": "unassessed"
        },
        {
          "name": "Spaceport Trench Tower D1 Locked Room",
          "technical": "DA_QuestArea_Spaceport_TrenchTowerD1_LockedRoom",
          "status": "unassessed"
        },
        {
          "name": "Spaceport Trench Tower D2",
          "technical": "DA_QuestArea_Spaceport_TrenchTowerD2",
          "status": "unassessed"
        },
        {
          "name": "Spaceport Trench Tower D2 Locked Room",
          "technical": "DA_QuestArea_Spaceport_TrenchTowerD2_LockedRoom",
          "status": "unassessed"
        },
        {
          "name": "Stella Montis",
          "technical": "DA_QuestArea_StellaMontis",
          "status": "unassessed"
        },
        {
          "name": "Stella Montis Medical Research",
          "technical": "DA_QuestArea_StellaMontis_MedicalResearch",
          "status": "unassessed"
        },
        {
          "name": "The Dam Admin Medical Bay",
          "technical": "DA_QuestArea_TheDam_Admin_MedicalBay",
          "status": "unassessed"
        },
        {
          "name": "The Dam Control Tower Room",
          "technical": "DA_QuestArea_TheDam_ControlTowerRoom",
          "status": "unassessed"
        },
        {
          "name": "The Dam Creek Water Source",
          "technical": "DA_QuestArea_TheDam_CreekWaterSource",
          "status": "unassessed"
        },
        {
          "name": "The Dam Generator Room",
          "technical": "DA_QuestArea_TheDam_GeneratorRoom",
          "status": "unassessed"
        },
        {
          "name": "The Dam Swamp",
          "technical": "DA_QuestArea_TheDam_Swamp",
          "status": "unassessed"
        },
        {
          "name": "The Dam Ventilation Shaft",
          "technical": "DA_QuestArea_TheDam_VentilationShaft",
          "status": "unassessed"
        },
        {
          "name": "The Dam Victory Ridge",
          "technical": "DA_QuestArea_TheDam_VictoryRidge",
          "status": "unassessed"
        },
        {
          "name": "The Dam Water Control Room",
          "technical": "DA_QuestArea_TheDam_WaterControlRoom",
          "status": "unassessed"
        },
        {
          "name": "The Storm Find Shelter",
          "technical": "DA_QuestArea_TheStorm_FindShelter",
          "status": "unassessed"
        },
        {
          "name": "Water Siphoning",
          "technical": "DA_QuestArea_WaterSiphoning",
          "status": "unassessed"
        },
        {
          "name": "Frozen Trails Collector Area 01",
          "technical": "DA_QuestArea_FrozenTrails_Collector_Area_01",
          "status": "unassessed"
        },
        {
          "name": "Frozen Trails Collector Area 02",
          "technical": "DA_QuestArea_FrozenTrails_Collector_Area_02",
          "status": "unassessed"
        },
        {
          "name": "Frozen Trails Collector Area 03",
          "technical": "DA_QuestArea_FrozenTrails_Collector_Area_03",
          "status": "unassessed"
        },
        {
          "name": "Frozen Trails Collector Area 03 Top",
          "technical": "DA_QuestArea_FrozenTrails_Collector_Area_03_Top",
          "status": "unassessed"
        },
        {
          "name": "Frozen Trails Collector Area 04",
          "technical": "DA_QuestArea_FrozenTrails_Collector_Area_04",
          "status": "unassessed"
        },
        {
          "name": "Frozen Trails Collector Area 05",
          "technical": "DA_QuestArea_FrozenTrails_Collector_Area_05",
          "status": "unassessed"
        },
        {
          "name": "Frozen Trails Collector Area 06",
          "technical": "DA_QuestArea_FrozenTrails_Collector_Area_06",
          "status": "unassessed"
        },
        {
          "name": "Frozen Trails Fallen ARCLeg 01",
          "technical": "DA_QuestArea_FrozenTrails_FallenARCLeg_01",
          "status": "unassessed"
        },
        {
          "name": "Frozen Trails Fallen ARCLeg 02",
          "technical": "DA_QuestArea_FrozenTrails_FallenARCLeg_02",
          "status": "unassessed"
        },
        {
          "name": "Frozen Trails Fallen ARCLeg 03",
          "technical": "DA_QuestArea_FrozenTrails_FallenARCLeg_03",
          "status": "unassessed"
        },
        {
          "name": "Frozen Trails Fallen ARCWreckage",
          "technical": "DA_QuestArea_FrozenTrails_FallenARCWreckage",
          "status": "unassessed"
        },
        {
          "name": "Frozen Trails Fallen Collector Balcony",
          "technical": "DA_QuestArea_FrozenTrails_FallenCollector_Balcony",
          "status": "unassessed"
        },
        {
          "name": "Frozen Trails Train Tracks South01",
          "technical": "DA_QuestArea_FrozenTrails_TrainTracks_South01",
          "status": "unassessed"
        },
        {
          "name": "Locked Gate Tunnel Interior",
          "technical": "DA_QuestArea_LockedGate_TunnelInterior",
          "status": "unassessed"
        },
        {
          "name": "BG Security Wing",
          "technical": "DA_QuestArea_BG_SecurityWing",
          "status": "unassessed"
        },
        {
          "name": "BG Security Wing Strong Room",
          "technical": "DA_QuestArea_BG_SecurityWing_StrongRoom",
          "status": "unassessed"
        },
        {
          "name": "Enelica Buildings",
          "technical": "DA_QuestArea_EnelicaBuildings",
          "status": "unassessed"
        },
        {
          "name": "Researchers Floor",
          "technical": "DA_QuestArea_ResearchersFloor",
          "status": "unassessed"
        },
        {
          "name": "Project Echo2 1",
          "technical": "DA_QuestArea_ProjectEcho2_1",
          "status": "unassessed"
        },
        {
          "name": "SM Assembly Control Rooms",
          "technical": "DA_QuestArea_SM_Assembly_ControlRooms",
          "status": "unassessed"
        },
        {
          "name": "SM Medical Area",
          "technical": "DA_QuestArea_SM_MedicalArea",
          "status": "unassessed"
        },
        {
          "name": "Blue Gate Keeping An Eye Out Hideout",
          "technical": "DA_QuestArea_BlueGate_KeepingAnEyeOut_Hideout",
          "status": "unassessed"
        },
        {
          "name": "Buried City Barricaded Room",
          "technical": "DA_QuestArea_BuriedCity_BarricadedRoom",
          "status": "unassessed"
        },
        {
          "name": "Buried City Main Street Depot",
          "technical": "DA_QuestArea_BuriedCity_MainStreetDepot",
          "status": "unassessed"
        },
        {
          "name": "Dam Power Control",
          "technical": "DA_QuestArea_Dam_PowerControl",
          "status": "unassessed"
        },
        {
          "name": "Spaceport Rocket Assembly",
          "technical": "DA_QuestArea_Spaceport_RocketAssembly",
          "status": "unassessed"
        },
        {
          "name": "Stella Montis Security Checkpoint",
          "technical": "DA_QuestArea_StellaMontis_SecurityCheckpoint",
          "status": "unassessed"
        },
        {
          "name": "Stella Montis Security Checkpoint Control Room",
          "technical": "DA_QuestArea_StellaMontis_SecurityCheckpoint_ControlRoom",
          "status": "unassessed"
        },
        {
          "name": "POI Field Depot Dust On The Wire",
          "technical": "DA_QuestArea_POI_FieldDepot_DustOnTheWire",
          "status": "unassessed"
        },
        {
          "name": "BC POI Derailed Train",
          "technical": "DA_QuestArea_BC_POI_DerailedTrain",
          "status": "unassessed"
        },
        {
          "name": "BC POI Market Ruins",
          "technical": "DA_QuestArea_BC_POI_MarketRuins",
          "status": "unassessed"
        },
        {
          "name": "BC POI Resistance HQ",
          "technical": "DA_QuestArea_BC_POI_ResistanceHQ",
          "status": "unassessed"
        },
        {
          "name": "Riven Tides POI",
          "technical": "DA_QuestArea_RivenTides_POI",
          "status": "unassessed"
        },
        {
          "name": "RT POI Safe Harbor Secret Dig Spot",
          "technical": "DA_QuestArea_RT_POI_SafeHarbor_SecretDigSpot",
          "status": "unassessed"
        },
        {
          "name": "RT POI Seabed",
          "technical": "DA_QuestArea_RT_POI_Seabed",
          "status": "unassessed"
        },
        {
          "name": "RT POI Wave Breaker Compound",
          "technical": "DA_QuestArea_RT_POI_Wave_Breaker_Compound",
          "status": "unassessed"
        },
        {
          "name": "BG Back Of Tunnel",
          "technical": "DA_QuestArea_BG_BackOfTunnel",
          "status": "unassessed"
        },
        {
          "name": "BG Home To Roost Locker Room",
          "technical": "DA_QuestArea_BG_HomeToRoost_LockerRoom",
          "status": "unassessed"
        },
        {
          "name": "Blue Gate Mantikor Office",
          "technical": "DA_QuestArea_BlueGate_Mantikor_Office",
          "status": "unassessed"
        },
        {
          "name": "Blue Gate Pilgrims Peak Antenna",
          "technical": "DA_QuestArea_BlueGate_PilgrimsPeak_Antenna",
          "status": "unassessed"
        },
        {
          "name": "Blue Gate Security Wing Control Room",
          "technical": "DA_QuestArea_BlueGate_SecurityWing_ControlRoom",
          "status": "unassessed"
        },
        {
          "name": "Frigate Volume Any Map",
          "technical": "DA_QuestArea_FrigateVolume_AnyMap",
          "status": "unassessed"
        },
        {
          "name": "Frigate Volume Blue Gate",
          "technical": "DA_QuestArea_FrigateVolume_BlueGate",
          "status": "unassessed"
        },
        {
          "name": "Frigate Volume Buried City",
          "technical": "DA_QuestArea_FrigateVolume_BuriedCity",
          "status": "unassessed"
        },
        {
          "name": "Frigate Volume Dam",
          "technical": "DA_QuestArea_FrigateVolume_Dam",
          "status": "unassessed"
        },
        {
          "name": "Frigate Volume Frozen Trail",
          "technical": "DA_QuestArea_FrigateVolume_FrozenTrail",
          "status": "unassessed"
        },
        {
          "name": "Frigate Volume Riven Tides",
          "technical": "DA_QuestArea_FrigateVolume_RivenTides",
          "status": "unassessed"
        },
        {
          "name": "Frigate Volume Spaceport",
          "technical": "DA_QuestArea_FrigateVolume_Spaceport",
          "status": "unassessed"
        },
        {
          "name": "Frozen Trails Emergency Shelter",
          "technical": "DA_QuestArea_FrozenTrails_EmergencyShelter",
          "status": "unassessed"
        },
        {
          "name": "Frozen Trails Fallen Collector",
          "technical": "DA_QuestArea_FrozenTrails_FallenCollector",
          "status": "unassessed"
        },
        {
          "name": "Frozen Trails Industrial East Bank",
          "technical": "DA_QuestArea_FrozenTrails_IndustrialEastBank",
          "status": "unassessed"
        },
        {
          "name": "Frozen Trails Industrial East Bank Gas Station",
          "technical": "DA_QuestArea_FrozenTrails_IndustrialEastBank_GasStation",
          "status": "unassessed"
        },
        {
          "name": "Frozen Trails Industrial East Bank Sub Station",
          "technical": "DA_QuestArea_FrozenTrails_IndustrialEastBank_SubStation",
          "status": "unassessed"
        },
        {
          "name": "Frozen Trails Loading Platform",
          "technical": "DA_QuestArea_FrozenTrails_LoadingPlatform",
          "status": "unassessed"
        },
        {
          "name": "Frozen Trails Main POI",
          "technical": "DA_QuestArea_FrozenTrails_MainPOI",
          "status": "unassessed"
        },
        {
          "name": "Frozen Trails Northen Tracks",
          "technical": "DA_QuestArea_FrozenTrails_NorthenTracks",
          "status": "unassessed"
        },
        {
          "name": "Frozen Trails Northen Tracks Researcher Lodgings",
          "technical": "DA_QuestArea_FrozenTrails_NorthenTracks_ResearcherLodgings",
          "status": "unassessed"
        },
        {
          "name": "Frozen Trails Observatory",
          "technical": "DA_QuestArea_FrozenTrails_Observatory",
          "status": "unassessed"
        },
        {
          "name": "Frozen Trails Observatory Hill",
          "technical": "DA_QuestArea_FrozenTrails_ObservatoryHill",
          "status": "unassessed"
        },
        {
          "name": "Frozen Trails Radio Tower",
          "technical": "DA_QuestArea_FrozenTrails_RadioTower",
          "status": "unassessed"
        },
        {
          "name": "Frozen Trails Railyard",
          "technical": "DA_QuestArea_FrozenTrails_Railyard",
          "status": "unassessed"
        },
        {
          "name": "Frozen Trails Railyard Sewers",
          "technical": "DA_QuestArea_FrozenTrails_Railyard_Sewers",
          "status": "unassessed"
        },
        {
          "name": "Frozen Trails Southern Pass",
          "technical": "DA_QuestArea_FrozenTrails_SouthernPass",
          "status": "unassessed"
        },
        {
          "name": "Frozen Trails Village",
          "technical": "DA_QuestArea_FrozenTrails_Village",
          "status": "unassessed"
        },
        {
          "name": "Frozen Trails Village Central Station",
          "technical": "DA_QuestArea_FrozenTrails_Village_CentralStation",
          "status": "unassessed"
        },
        {
          "name": "Frozen Trails Village Church",
          "technical": "DA_QuestArea_FrozenTrails_Village_Church",
          "status": "unassessed"
        },
        {
          "name": "Frozen Trails Village Old Town",
          "technical": "DA_QuestArea_FrozenTrails_Village_OldTown",
          "status": "unassessed"
        },
        {
          "name": "Frozen Trails Village Supermarket",
          "technical": "DA_QuestArea_FrozenTrails_Village_Supermarket",
          "status": "unassessed"
        },
        {
          "name": "Gondola Prototype Blue Gate",
          "technical": "DA_QuestArea_GondolaPrototype_BlueGate",
          "status": "unassessed"
        },
        {
          "name": "Gondola Prototype Dam",
          "technical": "DA_QuestArea_GondolaPrototype_Dam",
          "status": "unassessed"
        },
        {
          "name": "Gondola Prototype Riven Tides",
          "technical": "DA_QuestArea_GondolaPrototype_RivenTides",
          "status": "unassessed"
        },
        {
          "name": "Gondola Prototype Spaceport",
          "technical": "DA_QuestArea_GondolaPrototype_Spaceport",
          "status": "unassessed"
        },
        {
          "name": "Riven Tides Young Celestes Antenna",
          "technical": "DA_QuestArea_RivenTides_YoungCelestes_Antenna",
          "status": "unassessed"
        },
        {
          "name": "Riven Tides Young Celestes Hideout",
          "technical": "DA_QuestArea_RivenTides_YoungCelestes_Hideout",
          "status": "unassessed"
        },
        {
          "name": "Riven Tides Young Celestes Practice Area",
          "technical": "DA_QuestArea_RivenTides_YoungCelestes_PracticeArea",
          "status": "unassessed"
        },
        {
          "name": "Riven Tides Young Celestes Raider Camp",
          "technical": "DA_QuestArea_RivenTides_YoungCelestes_RaiderCamp",
          "status": "unassessed"
        },
        {
          "name": "Riven Tides Young Celestes Room",
          "technical": "DA_QuestArea_RivenTides_YoungCelestes_Room",
          "status": "unassessed"
        },
        {
          "name": "Rust Belt Mutiny5 Photo Arrival",
          "technical": "DA_QuestArea_RustBeltMutiny5_Photo_Arrival",
          "status": "unassessed"
        },
        {
          "name": "Rust Belt Mutiny5 Photo Broken Wall",
          "technical": "DA_QuestArea_RustBeltMutiny5_Photo_BrokenWall",
          "status": "unassessed"
        },
        {
          "name": "Rust Belt Mutiny5 Photo Bus",
          "technical": "DA_QuestArea_RustBeltMutiny5_Photo_Bus",
          "status": "unassessed"
        },
        {
          "name": "Rust Belt Mutiny5 Photo Launch Towers Control",
          "technical": "DA_QuestArea_RustBeltMutiny5_Photo_LaunchTowersControl",
          "status": "unassessed"
        },
        {
          "name": "Rust Belt Mutiny5 Photo Launch Towers Platform",
          "technical": "DA_QuestArea_RustBeltMutiny5_Photo_LaunchTowersPlatform",
          "status": "unassessed"
        },
        {
          "name": "Rust Belt Mutiny5 Photo Launch Towers Port A",
          "technical": "DA_QuestArea_RustBeltMutiny5_Photo_LaunchTowersPortA",
          "status": "unassessed"
        },
        {
          "name": "Rust Belt Mutiny5 Photo Security Booth",
          "technical": "DA_QuestArea_RustBeltMutiny5_Photo_SecurityBooth",
          "status": "unassessed"
        },
        {
          "name": "Rust Belt Mutiny5 Water Tower",
          "technical": "DA_QuestArea_RustBeltMutiny5_WaterTower",
          "status": "unassessed"
        },
        {
          "name": "Spaceport Hidden Bunker Entrance",
          "technical": "DA_QuestArea_Spaceport_HiddenBunker_Entrance",
          "status": "unassessed"
        },
        {
          "name": "Spaceport Hidden Bunker Flooded Cellar",
          "technical": "DA_QuestArea_Spaceport_HiddenBunker_FloodedCellar",
          "status": "unassessed"
        },
        {
          "name": "Spaceport Hidden Bunker Monitoring Room",
          "technical": "DA_QuestArea_Spaceport_HiddenBunker_MonitoringRoom",
          "status": "unassessed"
        },
        {
          "name": "Spaceport Hidden Bunker Power Room",
          "technical": "DA_QuestArea_Spaceport_HiddenBunker_PowerRoom",
          "status": "unassessed"
        },
        {
          "name": "Spaceport Hidden Bunker Server Hall",
          "technical": "DA_QuestArea_Spaceport_HiddenBunker_ServerHall",
          "status": "unassessed"
        },
        {
          "name": "Spaceport Hidden Bunker Sphere",
          "technical": "DA_QuestArea_Spaceport_HiddenBunker_Sphere",
          "status": "unassessed"
        },
        {
          "name": "Spaceport Trench Towers",
          "technical": "DA_QuestArea_Spaceport_TrenchTowers",
          "status": "unassessed"
        },
        {
          "name": "Stella Montis Hidden Bunker Comms",
          "technical": "DA_QuestArea_StellaMontis_HiddenBunker_Comms",
          "status": "unassessed"
        }
      ]
    },
    {
      "name": "Character skills",
      "children": [
        {
          "name": "Armor Speed Compensation",
          "technical": "DA_CharacterSkill_ArmorSpeedCompensation",
          "status": "unassessed"
        },
        {
          "name": "Breach Speed",
          "technical": "DA_CharacterSkill_BreachSpeed",
          "status": "unassessed"
        },
        {
          "name": "Climb Speed",
          "technical": "DA_CharacterSkill_ClimbSpeed",
          "status": "unassessed"
        },
        {
          "name": "Crouch Speed",
          "technical": "DA_CharacterSkill_CrouchSpeed",
          "status": "unassessed"
        },
        {
          "name": "Damage Stamina Boost",
          "technical": "DA_CharacterSkill_DamageStaminaBoost",
          "status": "unassessed"
        },
        {
          "name": "DBNOBleed Rate",
          "technical": "DA_CharacterSkill_DBNOBleedRate",
          "status": "unassessed"
        },
        {
          "name": "Dodge Roll Stamina Cost",
          "technical": "DA_CharacterSkill_DodgeRollStaminaCost",
          "status": "unassessed"
        },
        {
          "name": "Exhausted State Exit Threshold",
          "technical": "DA_CharacterSkill_ExhaustedStateExitThreshold",
          "status": "unassessed"
        },
        {
          "name": "Health Regen",
          "technical": "DA_CharacterSkill_HealthRegen",
          "status": "unassessed"
        },
        {
          "name": "Loot Reveal Speed",
          "technical": "DA_CharacterSkill_LootRevealSpeed",
          "status": "unassessed"
        },
        {
          "name": "Max Stamina",
          "technical": "DA_CharacterSkill_MaxStamina",
          "status": "unassessed"
        },
        {
          "name": "Melee Stamina Cost",
          "technical": "DA_CharacterSkill_MeleeStaminaCost",
          "status": "unassessed"
        },
        {
          "name": "Reduced Looting Noise",
          "technical": "DA_CharacterSkill_ReducedLootingNoise",
          "status": "unassessed"
        },
        {
          "name": "Sprint Dodge Roll Distance",
          "technical": "DA_CharacterSkill_SprintDodgeRollDistance",
          "status": "unassessed"
        },
        {
          "name": "Stamina Regen",
          "technical": "DA_CharacterSkill_StaminaRegen",
          "status": "unassessed"
        },
        {
          "name": "Traversal Stamina Cost",
          "technical": "DA_CharacterSkill_TraversalStaminaCost",
          "status": "unassessed"
        },
        {
          "name": "Armor Break Free Dodge Roll Oct Tree",
          "technical": "DA_CharacterSkill_ArmorBreakFreeDodgeRoll_OctTree",
          "status": "unassessed"
        },
        {
          "name": "Armor Speed Compensation Oct Tree",
          "technical": "DA_CharacterSkill_ArmorSpeedCompensation_OctTree",
          "status": "unassessed"
        },
        {
          "name": "Bonus Field Craft Recipes Oct Tree",
          "technical": "DA_CharacterSkill_BonusFieldCraftRecipes_OctTree",
          "status": "unassessed"
        },
        {
          "name": "Breach Gift Oct Tree",
          "technical": "DA_CharacterSkill_BreachGift_OctTree",
          "status": "unassessed"
        },
        {
          "name": "Breach Speed Oct Tree",
          "technical": "DA_CharacterSkill_BreachSpeed_OctTree",
          "status": "unassessed"
        },
        {
          "name": "Breach Volume Oct Tree",
          "technical": "DA_CharacterSkill_BreachVolume_OctTree",
          "status": "unassessed"
        },
        {
          "name": "Carryable Vaulting",
          "technical": "DA_CharacterSkill_CarryableVaulting",
          "status": "unassessed"
        },
        {
          "name": "Carry Weight Increase Oct Tree",
          "technical": "DA_CharacterSkill_CarryWeightIncrease_OctTree",
          "status": "unassessed"
        },
        {
          "name": "Climb Speed Oct Tree",
          "technical": "DA_CharacterSkill_ClimbSpeed_OctTree",
          "status": "unassessed"
        },
        {
          "name": "Crouched Stamina Regen Oct Tree",
          "technical": "DA_CharacterSkill_CrouchedStaminaRegen_OctTree",
          "status": "unassessed"
        },
        {
          "name": "Crouch Speed Oct Tree",
          "technical": "DA_CharacterSkill_CrouchSpeed_OctTree",
          "status": "unassessed"
        },
        {
          "name": "Damage Stamina Boost Oct Tree",
          "technical": "DA_CharacterSkill_DamageStaminaBoost_OctTree",
          "status": "unassessed"
        },
        {
          "name": "DBNOBleed Rate Oct Tree",
          "technical": "DA_CharacterSkill_DBNOBleedRate_OctTree",
          "status": "unassessed"
        },
        {
          "name": "DBNOMovement Speed Oct Tree",
          "technical": "DA_CharacterSkill_DBNOMovementSpeed_OctTree",
          "status": "unassessed"
        },
        {
          "name": "Disable Exhausted Vault Oct Tree",
          "technical": "DA_CharacterSkill_DisableExhaustedVault_OctTree",
          "status": "unassessed"
        },
        {
          "name": "Dodge Roll Stamina Cost Oct Tree",
          "technical": "DA_CharacterSkill_DodgeRollStaminaCost_OctTree",
          "status": "unassessed"
        },
        {
          "name": "Double Loot Reveal Oct Tree",
          "technical": "DA_CharacterSkill_DoubleLootReveal_OctTree",
          "status": "unassessed"
        },
        {
          "name": "Encumbered Move Speed",
          "technical": "DA_CharacterSkill_EncumberedMoveSpeed",
          "status": "unassessed"
        },
        {
          "name": "Encumbered Sprinting",
          "technical": "DA_CharacterSkill_EncumberedSprinting",
          "status": "unassessed"
        },
        {
          "name": "Exhausted State Exit Threshold Oct Tree",
          "technical": "DA_CharacterSkill_ExhaustedStateExitThreshold_OctTree",
          "status": "unassessed"
        },
        {
          "name": "Expert Scavenging",
          "technical": "DA_CharacterSkill_ExpertScavenging",
          "status": "unassessed"
        },
        {
          "name": "Fall Damage Oct Tree",
          "technical": "DA_CharacterSkill_FallDamage_OctTree",
          "status": "unassessed"
        },
        {
          "name": "Fall Height Grab Increase",
          "technical": "DA_CharacterSkill_FallHeightGrabIncrease",
          "status": "unassessed"
        },
        {
          "name": "Fall Recovery Duration",
          "technical": "DA_CharacterSkill_FallRecoveryDuration",
          "status": "unassessed"
        },
        {
          "name": "Field Craft Find Oct Tree",
          "technical": "DA_CharacterSkill_FieldCraftFind_OctTree",
          "status": "unassessed"
        },
        {
          "name": "Field Repair",
          "technical": "DA_CharacterSkill_FieldRepair",
          "status": "unassessed"
        },
        {
          "name": "Firearm Bash",
          "technical": "DA_CharacterSkill_FirearmBash",
          "status": "unassessed"
        },
        {
          "name": "Flow State",
          "technical": "DA_CharacterSkill_FlowState",
          "status": "unassessed"
        },
        {
          "name": "Healing Stamina Regen Oct Tree",
          "technical": "DA_CharacterSkill_HealingStaminaRegen_OctTree",
          "status": "unassessed"
        },
        {
          "name": "Health Regen Oct Tree",
          "technical": "DA_CharacterSkill_HealthRegen_OctTree",
          "status": "unassessed"
        },
        {
          "name": "Juggernaut",
          "technical": "DA_CharacterSkill_Juggernaut",
          "status": "unassessed"
        },
        {
          "name": "Level Two Breach Oct Tree",
          "technical": "DA_CharacterSkill_LevelTwoBreach_OctTree",
          "status": "unassessed"
        },
        {
          "name": "Loot Reveal Speed Oct Tree",
          "technical": "DA_CharacterSkill_LootRevealSpeed_OctTree",
          "status": "unassessed"
        },
        {
          "name": "Max Stamina Oct Tree",
          "technical": "DA_CharacterSkill_MaxStamina_OctTree",
          "status": "unassessed"
        },
        {
          "name": "Melee Damage To Drones Oct Tree",
          "technical": "DA_CharacterSkill_MeleeDamageToDrones_OctTree",
          "status": "unassessed"
        },
        {
          "name": "Melee Stamina Cost Oct Tree",
          "technical": "DA_CharacterSkill_MeleeStaminaCost_OctTree",
          "status": "unassessed"
        },
        {
          "name": "Mine Disarm Oct Tree",
          "technical": "DA_CharacterSkill_MineDisarm_OctTree",
          "status": "unassessed"
        },
        {
          "name": "Natural Adrenaline Shot",
          "technical": "DA_CharacterSkill_NaturalAdrenalineShot",
          "status": "unassessed"
        },
        {
          "name": "Ninja Roll",
          "technical": "DA_CharacterSkill_NinjaRoll",
          "status": "unassessed"
        },
        {
          "name": "Overencumbered Stamina Regen Oct Tree",
          "technical": "DA_CharacterSkill_OverencumberedStaminaRegen_OctTree",
          "status": "unassessed"
        },
        {
          "name": "Recovery Roll Height Oct Tree",
          "technical": "DA_CharacterSkill_RecoveryRollHeight_OctTree",
          "status": "unassessed"
        },
        {
          "name": "Recovery Roll Window Oct Tree",
          "technical": "DA_CharacterSkill_RecoveryRollWindow_OctTree",
          "status": "unassessed"
        },
        {
          "name": "Reduced DBNODamage Oct Tree",
          "technical": "DA_CharacterSkill_ReducedDBNODamage_OctTree",
          "status": "unassessed"
        },
        {
          "name": "Reduced Looting Noise Oct Tree",
          "technical": "DA_CharacterSkill_ReducedLootingNoise_OctTree",
          "status": "unassessed"
        },
        {
          "name": "Reduced Weapon Encumbrance Oct Tree",
          "technical": "DA_CharacterSkill_ReducedWeaponEncumbrance_OctTree",
          "status": "unassessed"
        },
        {
          "name": "Run Forrest",
          "technical": "DA_CharacterSkill_RunForrest",
          "status": "unassessed"
        },
        {
          "name": "Scramble State Duration",
          "technical": "DA_CharacterSkill_ScrambleStateDuration",
          "status": "unassessed"
        },
        {
          "name": "Slide Friction Oct Tree",
          "technical": "DA_CharacterSkill_SlideFriction_OctTree",
          "status": "unassessed"
        },
        {
          "name": "Sprint Boost Duration",
          "technical": "DA_CharacterSkill_SprintBoostDuration",
          "status": "unassessed"
        },
        {
          "name": "Stamina Free Vault Oct Tree",
          "technical": "DA_CharacterSkill_StaminaFreeVault_OctTree",
          "status": "unassessed"
        },
        {
          "name": "Super Field Crafting",
          "technical": "DA_CharacterSkill_SuperFieldCrafting",
          "status": "unassessed"
        },
        {
          "name": "Traversal Stamina Cost Oct Tree",
          "technical": "DA_CharacterSkill_TraversalStaminaCost_OctTree",
          "status": "unassessed"
        },
        {
          "name": "Unlock Field Craft Oct Tree",
          "technical": "DA_CharacterSkill_UnlockFieldCraft_OctTree",
          "status": "unassessed"
        },
        {
          "name": "Vault Spring Oct Tree",
          "technical": "DA_CharacterSkill_VaultSpring_OctTree",
          "status": "unassessed"
        },
        {
          "name": "Walking Stamina Regen Oct Tree",
          "technical": "DA_CharacterSkill_WalkingStaminaRegen_OctTree",
          "status": "unassessed"
        },
        {
          "name": "Wall Climb",
          "technical": "DA_CharacterSkill_WallClimb",
          "status": "unassessed"
        },
        {
          "name": "Wall Leap Distance Oct Tree",
          "technical": "DA_CharacterSkill_WallLeapDistance_OctTree",
          "status": "unassessed"
        },
        {
          "name": "Armor Break Free Dodge Roll",
          "technical": "DA_CharacterSkill_ArmorBreakFreeDodgeRoll",
          "status": "unassessed"
        },
        {
          "name": "Bonus Field Craft Recipes",
          "technical": "DA_CharacterSkill_BonusFieldCraftRecipes",
          "status": "unassessed"
        },
        {
          "name": "Breach Gift",
          "technical": "DA_CharacterSkill_BreachGift",
          "status": "unassessed"
        },
        {
          "name": "Breach Volume",
          "technical": "DA_CharacterSkill_BreachVolume",
          "status": "unassessed"
        },
        {
          "name": "Carry Weight Increase",
          "technical": "DA_CharacterSkill_CarryWeightIncrease",
          "status": "unassessed"
        },
        {
          "name": "Critical Health Movement Noise",
          "technical": "DA_CharacterSkill_CriticalHealthMovementNoise",
          "status": "unassessed"
        },
        {
          "name": "Critical Health Stamina Regen",
          "technical": "DA_CharacterSkill_CriticalHealthStaminaRegen",
          "status": "unassessed"
        },
        {
          "name": "Crouched Impaired AIVisibility",
          "technical": "DA_CharacterSkill_CrouchedImpairedAIVisibility",
          "status": "unassessed"
        },
        {
          "name": "Crouched Stamina Regen",
          "technical": "DA_CharacterSkill_CrouchedStaminaRegen",
          "status": "unassessed"
        },
        {
          "name": "DBNOMovement Speed",
          "technical": "DA_CharacterSkill_DBNOMovementSpeed",
          "status": "unassessed"
        },
        {
          "name": "Disable Exhausted Vault",
          "technical": "DA_CharacterSkill_DisableExhaustedVault",
          "status": "unassessed"
        },
        {
          "name": "Double Loot Reveal",
          "technical": "DA_CharacterSkill_DoubleLootReveal",
          "status": "unassessed"
        },
        {
          "name": "Explosion Intensity",
          "technical": "DA_CharacterSkill_ExplosionIntensity",
          "status": "unassessed"
        },
        {
          "name": "Fall Damage",
          "technical": "DA_CharacterSkill_FallDamage",
          "status": "unassessed"
        },
        {
          "name": "Field Craft Find",
          "technical": "DA_CharacterSkill_FieldCraftFind",
          "status": "unassessed"
        },
        {
          "name": "Healing Stamina Regen",
          "technical": "DA_CharacterSkill_HealingStaminaRegen",
          "status": "unassessed"
        },
        {
          "name": "Level Two Breach",
          "technical": "DA_CharacterSkill_LevelTwoBreach",
          "status": "unassessed"
        },
        {
          "name": "Melee Damage To Drones",
          "technical": "DA_CharacterSkill_MeleeDamageToDrones",
          "status": "unassessed"
        },
        {
          "name": "Melee One Hit Kill",
          "technical": "DA_CharacterSkill_MeleeOneHitKill",
          "status": "unassessed"
        },
        {
          "name": "Mine Disarm",
          "technical": "DA_CharacterSkill_MineDisarm",
          "status": "unassessed"
        },
        {
          "name": "Overencumbered Stamina Regen",
          "technical": "DA_CharacterSkill_OverencumberedStaminaRegen",
          "status": "unassessed"
        },
        {
          "name": "Recovery Roll Window",
          "technical": "DA_CharacterSkill_RecoveryRollWindow",
          "status": "unassessed"
        },
        {
          "name": "Reduced AIAccuracy",
          "technical": "DA_CharacterSkill_ReducedAIAccuracy",
          "status": "unassessed"
        },
        {
          "name": "Reduced DBNODamage",
          "technical": "DA_CharacterSkill_ReducedDBNODamage",
          "status": "unassessed"
        },
        {
          "name": "Reduced Weapon Encumbrance",
          "technical": "DA_CharacterSkill_ReducedWeaponEncumbrance",
          "status": "unassessed"
        },
        {
          "name": "Slide Friction",
          "technical": "DA_CharacterSkill_SlideFriction",
          "status": "unassessed"
        },
        {
          "name": "Sprint Dodge Roll Sprint Stamina",
          "technical": "DA_CharacterSkill_SprintDodgeRollSprintStamina",
          "status": "unassessed"
        },
        {
          "name": "Stamina Free Vault",
          "technical": "DA_CharacterSkill_StaminaFreeVault",
          "status": "unassessed"
        },
        {
          "name": "Unlock Field Craft",
          "technical": "DA_CharacterSkill_UnlockFieldCraft",
          "status": "unassessed"
        },
        {
          "name": "Vault Spring",
          "technical": "DA_CharacterSkill_VaultSpring",
          "status": "unassessed"
        },
        {
          "name": "Walking Stamina Regen",
          "technical": "DA_CharacterSkill_WalkingStaminaRegen",
          "status": "unassessed"
        },
        {
          "name": "Wall Leap Distance",
          "technical": "DA_CharacterSkill_WallLeapDistance",
          "status": "unassessed"
        },
        {
          "name": "Health Regen2",
          "technical": "DA_CharacterSkill_HealthRegen2",
          "status": "unassessed"
        },
        {
          "name": "Quickdraw",
          "technical": "DA_CharacterSkill_Quickdraw",
          "status": "unassessed"
        },
        {
          "name": "Anti Tick Field",
          "technical": "DA_CharacterSkill_AntiTickField",
          "status": "unassessed"
        },
        {
          "name": "Emergency ADDS",
          "technical": "DA_CharacterSkill_EmergencyADDS",
          "status": "unassessed"
        },
        {
          "name": "Full Body Tourniquet",
          "technical": "DA_CharacterSkill_FullBodyTourniquet",
          "status": "unassessed"
        },
        {
          "name": "Emergency Smoke",
          "technical": "DA_CharacterSkill_EmergencySmoke",
          "status": "unassessed"
        },
        {
          "name": "Health Regen1",
          "technical": "DA_CharacterSkill_HealthRegen1",
          "status": "unassessed"
        },
        {
          "name": "Mobile Repair",
          "technical": "DA_CharacterSkill_MobileRepair",
          "status": "unassessed"
        },
        {
          "name": "Revival Top Up",
          "technical": "DA_CharacterSkill_RevivalTopUp",
          "status": "unassessed"
        }
      ]
    },
    {
      "name": "Traders & NPCs",
      "children": [
        {
          "name": "NPC Apollo",
          "technical": "DA_Item_NPC_Apollo",
          "status": "unassessed"
        },
        {
          "name": "NPC Bilguun",
          "technical": "DA_Item_NPC_Bilguun",
          "status": "unassessed"
        },
        {
          "name": "NPC Celeste",
          "technical": "DA_Item_NPC_Celeste",
          "status": "unassessed"
        },
        {
          "name": "NPC Juanito",
          "technical": "DA_Item_NPC_Juanito",
          "status": "unassessed"
        },
        {
          "name": "NPC Lance",
          "technical": "DA_Item_NPC_Lance",
          "status": "unassessed"
        },
        {
          "name": "NPC Posh",
          "technical": "DA_Item_NPC_Posh",
          "status": "unassessed"
        },
        {
          "name": "NPC Shani",
          "technical": "DA_Item_NPC_Shani",
          "status": "unassessed"
        },
        {
          "name": "NPC Tian Wen",
          "technical": "DA_Item_NPC_TianWen",
          "status": "unassessed"
        }
      ]
    },
    {
      "name": "Outpost furniture",
      "children": [
        {
          "name": "Alpine Wardrobe 01",
          "technical": "DA_OutpostFurniture_AlpineWardrobe_01",
          "status": "unassessed"
        },
        {
          "name": "Bar Stool 01 A",
          "technical": "DA_OutpostFurniture_BarStool_01_A",
          "status": "unassessed"
        },
        {
          "name": "Bird Cage Project Table 01 A",
          "technical": "DA_OutpostFurniture_BirdCageProject_Table_01_A",
          "status": "unassessed"
        },
        {
          "name": "Bird Cage Project Table 02 A",
          "technical": "DA_OutpostFurniture_BirdCageProject_Table_02_A",
          "status": "unassessed"
        },
        {
          "name": "CA Lamp Floor 02 A",
          "technical": "DA_OutpostFurniture_CA_LampFloor_02_A",
          "status": "unassessed"
        },
        {
          "name": "Chambers Bed Table 01 A",
          "technical": "DA_OutpostFurniture_Chambers_BedTable_01_A",
          "status": "unassessed"
        },
        {
          "name": "Chambers Lamp 01 A",
          "technical": "DA_OutpostFurniture_Chambers_Lamp_01_A",
          "status": "unassessed"
        },
        {
          "name": "Chambers Shelf 01 A",
          "technical": "DA_OutpostFurniture_Chambers_Shelf_01_A",
          "status": "unassessed"
        },
        {
          "name": "Chandelier 01 A",
          "technical": "DA_OutpostFurniture_Chandelier_01_A",
          "status": "unassessed"
        },
        {
          "name": "Cmr Cupboard Cafe 01 A",
          "technical": "DA_OutpostFurniture_Cmr_CupboardCafe_01_A",
          "status": "unassessed"
        },
        {
          "name": "Cmr Lamp Ceiling 01 A",
          "technical": "DA_OutpostFurniture_Cmr_LampCeiling_01_A",
          "status": "unassessed"
        },
        {
          "name": "Cmr Shelf 03 A",
          "technical": "DA_OutpostFurniture_Cmr_Shelf_03_A",
          "status": "unassessed"
        },
        {
          "name": "Cmr Sofa 03 A",
          "technical": "DA_OutpostFurniture_Cmr_Sofa_03_A",
          "status": "unassessed"
        },
        {
          "name": "Console Chest 01 A",
          "technical": "DA_OutpostFurniture_ConsoleChest_01_A",
          "status": "unassessed"
        },
        {
          "name": "Construction Light 01 A",
          "technical": "DA_OutpostFurniture_ConstructionLight_01_A",
          "status": "unassessed"
        },
        {
          "name": "DA Outpost Furniture Bar Table 01 A",
          "technical": "DA_Outpost_Furniture_BarTable_01_A",
          "status": "unassessed"
        },
        {
          "name": "DA Outpost Furniture Cafeteria Table 01 A",
          "technical": "DA_Outpost_Furniture_CafeteriaTable_01_A",
          "status": "unassessed"
        },
        {
          "name": "DA Outpost Furniture Cmr Chair 01 A",
          "technical": "DA_Outpost_Furniture_Cmr_Chair_01_A",
          "status": "unassessed"
        },
        {
          "name": "DA Outpost Furniture Cmr Chair 03 A",
          "technical": "DA_Outpost_Furniture_Cmr_Chair_03_A",
          "status": "unassessed"
        },
        {
          "name": "DA Outpost Furniture Cmr Table 02 A",
          "technical": "DA_Outpost_Furniture_Cmr_Table_02_A",
          "status": "unassessed"
        },
        {
          "name": "DA Outpost Furniture Gvt Chair 01",
          "technical": "DA_Outpost_Furniture_Gvt_Chair_01",
          "status": "unassessed"
        },
        {
          "name": "DA Outpost Furniture Gvt Chair 02 A",
          "technical": "DA_Outpost_Furniture_Gvt_Chair_02_A",
          "status": "unassessed"
        },
        {
          "name": "DA Outpost Furniture HOR Chair 01 A",
          "technical": "DA_Outpost_Furniture_HOR_Chair_01_A",
          "status": "unassessed"
        },
        {
          "name": "DA Outpost Furniture Res Armchair 01 A",
          "technical": "DA_Outpost_Furniture_Res_Armchair_01_A",
          "status": "unassessed"
        },
        {
          "name": "DA Outpost Furniture Res Chair 02 A",
          "technical": "DA_Outpost_Furniture_Res_Chair_02_A",
          "status": "unassessed"
        },
        {
          "name": "DA Outpost Furniture Res Chair 03 A",
          "technical": "DA_Outpost_Furniture_Res_Chair_03_A",
          "status": "unassessed"
        },
        {
          "name": "DA Outpost Furniture Res Table 01 A",
          "technical": "DA_Outpost_Furniture_Res_Table_01_A",
          "status": "unassessed"
        },
        {
          "name": "DA Outpost Furniture Res Table 02 A",
          "technical": "DA_Outpost_Furniture_Res_Table_02_A",
          "status": "unassessed"
        },
        {
          "name": "DA Outpost Furniture Res Table 03 A",
          "technical": "DA_Outpost_Furniture_Res_Table_03_A",
          "status": "unassessed"
        },
        {
          "name": "DA Outpost Furniture Rsr Chair 01 A",
          "technical": "DA_Outpost_Furniture_Rsr_Chair_01_A",
          "status": "unassessed"
        },
        {
          "name": "DA Outpost Furniture Rsr Chair 02 A",
          "technical": "DA_Outpost_Furniture_Rsr_Chair_02_A",
          "status": "unassessed"
        },
        {
          "name": "DA Outpost Furniture RST Table 01 A",
          "technical": "DA_Outpost_Furniture_RST_Table_01_A",
          "status": "unassessed"
        },
        {
          "name": "DA Outpost Furniture Sct Chair 01 A",
          "technical": "DA_Outpost_Furniture_Sct_Chair_01_A",
          "status": "unassessed"
        },
        {
          "name": "DA Outpost Furniture Wrh Chair 01 A",
          "technical": "DA_Outpost_Furniture_Wrh_Chair_01_A",
          "status": "unassessed"
        },
        {
          "name": "Debris Draped 01",
          "technical": "DA_OutpostFurniture_DebrisDraped_01",
          "status": "unassessed"
        },
        {
          "name": "Debris Draped 02",
          "technical": "DA_OutpostFurniture_DebrisDraped_02",
          "status": "unassessed"
        },
        {
          "name": "Debris Draped 03",
          "technical": "DA_OutpostFurniture_DebrisDraped_03",
          "status": "unassessed"
        },
        {
          "name": "Debris Draped 04",
          "technical": "DA_OutpostFurniture_DebrisDraped_04",
          "status": "unassessed"
        },
        {
          "name": "Debris Draped 05",
          "technical": "DA_OutpostFurniture_DebrisDraped_05",
          "status": "unassessed"
        },
        {
          "name": "Debris Draped 06",
          "technical": "DA_OutpostFurniture_DebrisDraped_06",
          "status": "unassessed"
        },
        {
          "name": "Floor Lamp 02 A",
          "technical": "DA_OutpostFurniture_FloorLamp_02_A",
          "status": "unassessed"
        },
        {
          "name": "Glass Ceiling Lamp 01 A",
          "technical": "DA_OutpostFurniture_GlassCeilingLamp_01_A",
          "status": "unassessed"
        },
        {
          "name": "Gvt Lamp Ceiling 01 A",
          "technical": "DA_OutpostFurniture_Gvt_LampCeiling_01_A",
          "status": "unassessed"
        },
        {
          "name": "Gvt Sofa 01 A",
          "technical": "DA_OutpostFurniture_Gvt_Sofa_01_A",
          "status": "unassessed"
        },
        {
          "name": "Gvt Sofa 02 A",
          "technical": "DA_OutpostFurniture_Gvt_Sofa_02_A",
          "status": "unassessed"
        },
        {
          "name": "Gvt Sofa 03 A",
          "technical": "DA_OutpostFurniture_Gvt_Sofa_03_A",
          "status": "unassessed"
        },
        {
          "name": "Hme Stool 01 A",
          "technical": "DA_OutpostFurniture_Hme_Stool_01_A",
          "status": "unassessed"
        },
        {
          "name": "Offerings Console",
          "technical": "DA_OutpostFurniture_OfferingsConsole",
          "status": "unassessed"
        },
        {
          "name": "Project Winter Table 01 A",
          "technical": "DA_OutpostFurniture_ProjectWinter_Table_01_A",
          "status": "unassessed"
        },
        {
          "name": "DA Outpost Furniture Raider Den Bed 01",
          "technical": "DA_Outpost_Furniture_RaiderDen_Bed_01",
          "status": "unassessed"
        },
        {
          "name": "DA Outpost Furniture Raider Den Bed 02",
          "technical": "DA_Outpost_Furniture_RaiderDen_Bed_02",
          "status": "unassessed"
        },
        {
          "name": "DA Outpost Furniture Raider Den Bed 03",
          "technical": "DA_Outpost_Furniture_RaiderDen_Bed_03",
          "status": "unassessed"
        },
        {
          "name": "DA Outpost Furniture Raider Den Bed 04",
          "technical": "DA_Outpost_Furniture_RaiderDen_Bed_04",
          "status": "unassessed"
        },
        {
          "name": "Raider Den Shelves 01",
          "technical": "DA_OutpostFurniture_RaiderDen_Shelves_01",
          "status": "unassessed"
        },
        {
          "name": "Raider Den Shelves 02",
          "technical": "DA_OutpostFurniture_RaiderDen_Shelves_02",
          "status": "unassessed"
        },
        {
          "name": "Raider Den Shelves 03",
          "technical": "DA_OutpostFurniture_RaiderDen_Shelves_03",
          "status": "unassessed"
        },
        {
          "name": "Raider Den Shelves 04",
          "technical": "DA_OutpostFurniture_RaiderDen_Shelves_04",
          "status": "unassessed"
        },
        {
          "name": "Res Ceiling Lamp 01 A",
          "technical": "DA_OutpostFurniture_Res_CeilingLamp_01_A",
          "status": "unassessed"
        },
        {
          "name": "Res Closet 01 A",
          "technical": "DA_OutpostFurniture_Res_Closet_01_A",
          "status": "unassessed"
        },
        {
          "name": "Res Drawer 01 A",
          "technical": "DA_OutpostFurniture_Res_Drawer_01_A",
          "status": "unassessed"
        },
        {
          "name": "Res Drawer Medical 01 A",
          "technical": "DA_OutpostFurniture_Res_DrawerMedical_01_A",
          "status": "unassessed"
        },
        {
          "name": "Res Piano 01 A",
          "technical": "DA_OutpostFurniture_Res_Piano_01_A",
          "status": "unassessed"
        },
        {
          "name": "Res Sofa 01 Y150 A",
          "technical": "DA_OutpostFurniture_Res_Sofa_01_Y150_A",
          "status": "unassessed"
        },
        {
          "name": "Res Sofa 01 Y150 A Patterns",
          "technical": "DA_OutpostFurniture_Res_Sofa_01_Y150_A_Patterns",
          "status": "unassessed"
        },
        {
          "name": "Res Sofa 01 Y150 A Patterns Geo",
          "technical": "DA_OutpostFurniture_Res_Sofa_01_Y150_A_Patterns_Geo",
          "status": "unassessed"
        },
        {
          "name": "Res Sofa 01 Y150 A Patterns Green Grometry",
          "technical": "DA_OutpostFurniture_Res_Sofa_01_Y150_A_Patterns_Green_Grometry",
          "status": "unassessed"
        },
        {
          "name": "Res Sofa 01 Y150 A Research Company BC",
          "technical": "DA_OutpostFurniture_Res_Sofa_01_Y150_A_ResearchCompany_BC",
          "status": "unassessed"
        },
        {
          "name": "Res Sofa 01 Y300 A",
          "technical": "DA_OutpostFurniture_Res_Sofa_01_Y300_A",
          "status": "unassessed"
        },
        {
          "name": "Res Sofa 01 Y300 A Patterns",
          "technical": "DA_OutpostFurniture_Res_Sofa_01_Y300_A_Patterns",
          "status": "unassessed"
        },
        {
          "name": "Res Sofa 01 Y300 A Patterns Geo",
          "technical": "DA_OutpostFurniture_Res_Sofa_01_Y300_A_Patterns_Geo",
          "status": "unassessed"
        },
        {
          "name": "Res Sofa 01 Y300 A Patterns Green Grometry",
          "technical": "DA_OutpostFurniture_Res_Sofa_01_Y300_A_Patterns_Green_Grometry",
          "status": "unassessed"
        },
        {
          "name": "Res Sofa 01 Y300 A Research Company BC",
          "technical": "DA_OutpostFurniture_Res_Sofa_01_Y300_A_ResearchCompany_BC",
          "status": "unassessed"
        },
        {
          "name": "Res Sofa 03 A",
          "technical": "DA_OutpostFurniture_Res_Sofa_03_A",
          "status": "unassessed"
        },
        {
          "name": "Res Tv Unit 01 A",
          "technical": "DA_OutpostFurniture_Res_TvUnit_01_A",
          "status": "unassessed"
        },
        {
          "name": "Rsr Cabinet M 02 X100cm A",
          "technical": "DA_OutpostFurniture_Rsr_Cabinet_M_02_X100cm_A",
          "status": "unassessed"
        },
        {
          "name": "Rsr Cabinet M 02 X100cm B",
          "technical": "DA_OutpostFurniture_Rsr_Cabinet_M_02_X100cm_B",
          "status": "unassessed"
        },
        {
          "name": "Rsr Cabinet M 02 X200cm A",
          "technical": "DA_OutpostFurniture_Rsr_Cabinet_M_02_X200cm_A",
          "status": "unassessed"
        },
        {
          "name": "Rsr Cabinet M 02 X200cm B",
          "technical": "DA_OutpostFurniture_Rsr_Cabinet_M_02_X200cm_B",
          "status": "unassessed"
        },
        {
          "name": "Rsr Cabinet S 01 B",
          "technical": "DA_OutpostFurniture_Rsr_Cabinet_S_01_B",
          "status": "unassessed"
        },
        {
          "name": "Rsr Cabinet S 02 A Blue",
          "technical": "DA_OutpostFurniture_Rsr_Cabinet_S_02_A_Blue",
          "status": "unassessed"
        },
        {
          "name": "Rsr Ceiling Light 01 A",
          "technical": "DA_OutpostFurniture_Rsr_CeilingLight_01_A",
          "status": "unassessed"
        },
        {
          "name": "Rsr Stool 01 A",
          "technical": "DA_OutpostFurniture_Rsr_Stool_01_A",
          "status": "unassessed"
        },
        {
          "name": "Shelves 02 A",
          "technical": "DA_OutpostFurniture_Shelves_02_A",
          "status": "unassessed"
        },
        {
          "name": "Shelves 02 B",
          "technical": "DA_OutpostFurniture_Shelves_02_B",
          "status": "unassessed"
        },
        {
          "name": "Sofa 01 A",
          "technical": "DA_OutpostFurniture_Sofa_01_A",
          "status": "unassessed"
        },
        {
          "name": "South Forest Bush Holly 01 C",
          "technical": "DA_OutpostFurniture_South_Forest_Bush_Holly_01_C",
          "status": "unassessed"
        },
        {
          "name": "Stash 01 A",
          "technical": "DA_OutpostFurniture_Stash_01_A",
          "status": "unassessed"
        },
        {
          "name": "Stash 01 B",
          "technical": "DA_OutpostFurniture_Stash_01_B",
          "status": "unassessed"
        },
        {
          "name": "Stash 01 C",
          "technical": "DA_OutpostFurniture_Stash_01_C",
          "status": "unassessed"
        },
        {
          "name": "Stash 01 D",
          "technical": "DA_OutpostFurniture_Stash_01_D",
          "status": "unassessed"
        },
        {
          "name": "Venetian Dresser 01 A",
          "technical": "DA_OutpostFurniture_VenetianDresser_01_A",
          "status": "unassessed"
        },
        {
          "name": "Wardrobe 01 B",
          "technical": "DA_OutpostFurniture_Wardrobe_01_B",
          "status": "unassessed"
        },
        {
          "name": "Wardrobe 01 C",
          "technical": "DA_OutpostFurniture_Wardrobe_01_C",
          "status": "unassessed"
        },
        {
          "name": "Wardrobe 01 D",
          "technical": "DA_OutpostFurniture_Wardrobe_01_D",
          "status": "unassessed"
        },
        {
          "name": "Weapon Display 01 A",
          "technical": "DA_OutpostFurniture_WeaponDisplay_01_A",
          "status": "unassessed"
        },
        {
          "name": "Weapon Display 01 B",
          "technical": "DA_OutpostFurniture_WeaponDisplay_01_B",
          "status": "unassessed"
        },
        {
          "name": "Weapon Display 01 C",
          "technical": "DA_OutpostFurniture_WeaponDisplay_01_C",
          "status": "unassessed"
        },
        {
          "name": "Weather Monitor 01 Chair",
          "technical": "DA_OutpostFurniture_WeatherMonitor_01_Chair",
          "status": "unassessed"
        },
        {
          "name": "Weather Monitor 01 Table",
          "technical": "DA_OutpostFurniture_WeatherMonitor_01_Table",
          "status": "unassessed"
        },
        {
          "name": "Wrh Desk 01 A",
          "technical": "DA_OutpostFurniture_Wrh_Desk_01_A",
          "status": "unassessed"
        },
        {
          "name": "Wrh Drawers 01 A",
          "technical": "DA_OutpostFurniture_Wrh_Drawers_01_A",
          "status": "unassessed"
        }
      ]
    },
    {
      "name": "Appearance",
      "children": [
        {
          "name": "Skins & accessories",
          "children": [
            {
              "name": "Backpack Attachment Antenna",
              "technical": "DA_OI_BackpackAttachment_Antenna",
              "status": "unassessed"
            },
            {
              "name": "Backpack Attachment Aviator Mask 01 A",
              "technical": "DA_OI_BackpackAttachment_AviatorMask_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Attachment Bird House 01 A",
              "technical": "DA_OI_BackpackAttachment_BirdHouse_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Attachment Blanket Roll",
              "technical": "DA_OI_BackpackAttachment_BlanketRoll",
              "status": "unassessed"
            },
            {
              "name": "Backpack Attachment Boxing Gloves 01 A",
              "technical": "DA_OI_BackpackAttachment_BoxingGloves_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Attachment Briefcase 01 A",
              "technical": "DA_OI_BackpackAttachment_Briefcase_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Attachment Bucket 01 A",
              "technical": "DA_OI_BackpackAttachment_Bucket_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Attachment Can Six Pack 01 A",
              "technical": "DA_OI_BackpackAttachment_CanSixPack_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Attachment Canteen 01 A",
              "technical": "DA_OI_BackpackAttachment_Canteen_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Attachment Carbide Lamp 01 A",
              "technical": "DA_OI_BackpackAttachment_CarbideLamp_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Attachment Cauldron 01 A",
              "technical": "DA_OI_BackpackAttachment_Cauldron_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Attachment Ceramic Vase 01 A",
              "technical": "DA_OI_BackpackAttachment_CeramicVase_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Attachment Circular Saw 01 A",
              "technical": "DA_OI_BackpackAttachment_CircularSaw_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Attachment Diving Fins",
              "technical": "DA_OI_BackpackAttachment_DivingFins",
              "status": "unassessed"
            },
            {
              "name": "Backpack Attachment Drum",
              "technical": "DA_OI_BackpackAttachment_Drum",
              "status": "unassessed"
            },
            {
              "name": "Backpack Attachment Fax Machine 01 A",
              "technical": "DA_OI_BackpackAttachment_FaxMachine_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Attachment Fax Machine Dark 01 A",
              "technical": "DA_OI_BackpackAttachment_FaxMachineDark_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Attachment Field Kitchen 01 A",
              "technical": "DA_OI_BackpackAttachment_FieldKitchen_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Attachment First Aid Kit",
              "technical": "DA_OI_BackpackAttachment_FirstAidKit",
              "status": "unassessed"
            },
            {
              "name": "Backpack Attachment Fish Trap 01 A",
              "technical": "DA_OI_BackpackAttachment_FishTrap_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Attachment Flight Recorder 01 A",
              "technical": "DA_OI_BackpackAttachment_FlightRecorder_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Attachment Football 01 A",
              "technical": "DA_OI_BackpackAttachment_Football_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Attachment Fuel Canister 01 A",
              "technical": "DA_OI_BackpackAttachment_FuelCanister_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Attachment Game Console 01 A",
              "technical": "DA_OI_BackpackAttachment_GameConsole_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Attachment Gas Canister 01 A",
              "technical": "DA_OI_BackpackAttachment_GasCanister_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Attachment Gas Canister 02 A",
              "technical": "DA_OI_BackpackAttachment_GasCanister_02_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Attachment Gasoline Can 01 A",
              "technical": "DA_OI_BackpackAttachment_GasolineCan_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Attachment Gauge Box 01 A",
              "technical": "DA_OI_BackpackAttachment_GaugeBox_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Attachment Globe 01 A",
              "technical": "DA_OI_BackpackAttachment_Globe_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Attachment Grain Sack 01 A",
              "technical": "DA_OI_BackpackAttachment_GrainSack_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Attachment Hookah",
              "technical": "DA_OI_BackpackAttachment_Hookah",
              "status": "unassessed"
            },
            {
              "name": "Backpack Attachment Hose Bundle",
              "technical": "DA_OI_BackpackAttachment_HoseBundle",
              "status": "unassessed"
            },
            {
              "name": "Backpack Attachment Hydraulic Pump 01 A",
              "technical": "DA_OI_BackpackAttachment_HydraulicPump_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Attachment Infrared Transmitter 01 A",
              "technical": "DA_OI_BackpackAttachment_InfraredTransmitter_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Attachment Keyboard 01 A",
              "technical": "DA_OI_BackpackAttachment_Keyboard_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Attachment Lithium Pack 01 A",
              "technical": "DA_OI_BackpackAttachment_LithiumPack_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Attachment Mailbox 01 A",
              "technical": "DA_OI_BackpackAttachment_Mailbox_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Attachment Maps 01 A",
              "technical": "DA_OI_BackpackAttachment_Maps_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Attachment Mine",
              "technical": "DA_OI_BackpackAttachment_Mine",
              "status": "unassessed"
            },
            {
              "name": "Backpack Attachment Nomad Jug 01 A",
              "technical": "DA_OI_BackpackAttachment_NomadJug_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Attachment None",
              "technical": "DA_OI_BackpackAttachment_None",
              "status": "unassessed"
            },
            {
              "name": "Backpack Attachment Oxygen Respirator 01 A",
              "technical": "DA_OI_BackpackAttachment_OxygenRespirator_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Attachment Panettone 01 A",
              "technical": "DA_OI_BackpackAttachment_Panettone_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Attachment Picnic Basket 01 A",
              "technical": "DA_OI_BackpackAttachment_PicnicBasket_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Attachment Pocket Extinguisher 01 A",
              "technical": "DA_OI_BackpackAttachment_PocketExtinguisher_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Attachment Radiopack 01 A",
              "technical": "DA_OI_BackpackAttachment_Radiopack_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Attachment Radiopack 02 A",
              "technical": "DA_OI_BackpackAttachment_Radiopack_02_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Attachment Record Player 01",
              "technical": "DA_OI_BackpackAttachment_RecordPlayer_01",
              "status": "unassessed"
            },
            {
              "name": "Backpack Attachment Renegade Censer 01 A",
              "technical": "DA_OI_BackpackAttachment_RenegadeCenser_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Attachment Rocket Ship",
              "technical": "DA_OI_BackpackAttachment_RocketShip",
              "status": "unassessed"
            },
            {
              "name": "Backpack Attachment Sample Box 01 A",
              "technical": "DA_OI_BackpackAttachment_SampleBox_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Attachment Scrolls 01 A",
              "technical": "DA_OI_BackpackAttachment_Scrolls_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Attachment Ship Bell 01 A",
              "technical": "DA_OI_BackpackAttachment_ShipBell_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Attachment Shoes 01 A",
              "technical": "DA_OI_BackpackAttachment_Shoes_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Attachment Snow Rescue Kit",
              "technical": "DA_OI_BackpackAttachment_SnowRescueKit",
              "status": "unassessed"
            },
            {
              "name": "Backpack Attachment Spacecraft Module 01 A",
              "technical": "DA_OI_BackpackAttachment_SpacecraftModule_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Attachment Spinning Top 01 A",
              "technical": "DA_OI_BackpackAttachment_Spinning_Top_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Attachment Tick Trophy 01 A",
              "technical": "DA_OI_BackpackAttachment_TickTrophy_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Attachment Tool Bag 01 A",
              "technical": "DA_OI_BackpackAttachment_ToolBag_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Attachment Train Whistle 01 A",
              "technical": "DA_OI_BackpackAttachment_TrainWhistle_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Attachment Waterskin 01 A",
              "technical": "DA_OI_BackpackAttachment_Waterskin_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Attachment Weapons Case",
              "technical": "DA_OI_BackpackAttachment_WeaponsCase",
              "status": "unassessed"
            },
            {
              "name": "Backpack Attachment Wicker Basket 01 A",
              "technical": "DA_OI_BackpackAttachment_WickerBasket_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Charm Anchor 01 A",
              "technical": "DA_OI_BackpackCharm_Anchor_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Charm Anemometer 01",
              "technical": "DA_OI_BackpackCharm_Anemometer_01",
              "status": "unassessed"
            },
            {
              "name": "Backpack Charm Antler Talisman 01 A",
              "technical": "DA_OI_BackpackCharm_AntlerTalisman_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Charm Banana 01 A",
              "technical": "DA_OI_BackpackCharm_Banana_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Charm Baseball 01",
              "technical": "DA_OI_BackpackCharm_Baseball_01",
              "status": "unassessed"
            },
            {
              "name": "Backpack Charm Binoculars 01 A",
              "technical": "DA_OI_BackpackCharm_Binoculars_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Charm Bobble Head 01 A",
              "technical": "DA_OI_BackpackCharm_BobbleHead_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Charm Bolt Buddies 01",
              "technical": "DA_OI_BackpackCharm_BoltBuddies_01",
              "status": "unassessed"
            },
            {
              "name": "Backpack Charm Bone Saw 01",
              "technical": "DA_OI_BackpackCharm_BoneSaw_01",
              "status": "unassessed"
            },
            {
              "name": "Backpack Charm Brass Tap 01",
              "technical": "DA_OI_BackpackCharm_BrassTap_01",
              "status": "unassessed"
            },
            {
              "name": "Backpack Charm Burger Plushie 01 A",
              "technical": "DA_OI_BackpackCharm_BurgerPlushie_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Charm Cactus Pot 01 A",
              "technical": "DA_OI_BackpackCharm_CactusPot_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Charm Canned Tomatoes 01 A",
              "technical": "DA_OI_BackpackCharm_CannedTomatoes_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Charm Car Freshner 01 A",
              "technical": "DA_OI_BackpackCharm_CarFreshner_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Charm Casette Player 01 A",
              "technical": "DA_OI_BackpackCharm_CasettePlayer_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Charm Cassette 01 A",
              "technical": "DA_OI_BackpackCharm_Cassette_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Charm Chattering Teeth",
              "technical": "DA_OI_BackpackCharm_ChatteringTeeth",
              "status": "unassessed"
            },
            {
              "name": "Backpack Charm Christmas Horn 01 A",
              "technical": "DA_OI_BackpackCharm_ChristmasHorn_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Charm Coffee Pot 01 A",
              "technical": "DA_OI_BackpackCharm_CoffeePot_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Charm Computer Mouse 01 A",
              "technical": "DA_OI_BackpackCharm_ComputerMouse_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Charm Computer Mouse Dark 01 A",
              "technical": "DA_OI_BackpackCharm_ComputerMouseDark_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Charm Conch Shell 01 A",
              "technical": "DA_OI_BackpackCharm_ConchShell_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Charm Cow Bell 01 A",
              "technical": "DA_OI_BackpackCharm_CowBell_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Charm Deer Skull 01 A",
              "technical": "DA_OI_BackpackCharm_DeerSkull_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Charm Depth Gauge",
              "technical": "DA_OI_BackpackCharm_DepthGauge",
              "status": "unassessed"
            },
            {
              "name": "Backpack Charm Diving Goggles",
              "technical": "DA_OI_BackpackCharm_DivingGoggles",
              "status": "unassessed"
            },
            {
              "name": "Backpack Charm Enamel 01 A",
              "technical": "DA_OI_BackpackCharm_Enamel_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Charm Field Glass 01 A",
              "technical": "DA_OI_BackpackCharm_FieldGlass_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Charm Film Reel 01 A",
              "technical": "DA_OI_BackpackCharm_FilmReel_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Charm Fish 01 A",
              "technical": "DA_OI_BackpackCharm_Fish_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Charm Floater 01 A",
              "technical": "DA_OI_BackpackCharm_Floater_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Charm Floppy Disk 01 A",
              "technical": "DA_OI_BackpackCharm_FloppyDisk_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Charm Gear Bracelet 01 A",
              "technical": "DA_OI_BackpackCharm_GearBracelet_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Charm Glowstick Bundle 01 A",
              "technical": "DA_OI_BackpackCharm_GlowstickBundle_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Charm Gourd 01 A",
              "technical": "DA_OI_BackpackCharm_Gourd_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Charm Handheld Lamp 01",
              "technical": "DA_OI_BackpackCharm_HandheldLamp_01",
              "status": "unassessed"
            },
            {
              "name": "Backpack Charm Harmonica 01 A",
              "technical": "DA_OI_BackpackCharm_Harmonica_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Charm Hatchlingchick",
              "technical": "DA_OI_BackpackCharm_Hatchlingchick",
              "status": "unassessed"
            },
            {
              "name": "Backpack Charm Highway Torch",
              "technical": "DA_OI_BackpackCharm_HighwayTorch",
              "status": "unassessed"
            },
            {
              "name": "Backpack Charm Horse 01",
              "technical": "DA_OI_BackpackCharm_Horse_01",
              "status": "unassessed"
            },
            {
              "name": "Backpack Charm Hot Air Balloon 01 A",
              "technical": "DA_OI_BackpackCharm_HotAirBalloon_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Charm Ice Screws Bundle 01 A",
              "technical": "DA_OI_BackpackCharm_IceScrewsBundle_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Charm Joystick 01 A",
              "technical": "DA_OI_BackpackCharm_Joystick_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Charm Kill Counter 01 A",
              "technical": "DA_OI_BackpackCharm_KillCounter_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Charm Lance Key Chain 01 A",
              "technical": "DA_OI_BackpackCharm_LanceKeyChain_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Charm Lemons 01 A",
              "technical": "DA_OI_BackpackCharm_Lemons_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Charm Lizzie 01 A",
              "technical": "DA_OI_BackpackCharm_Lizzie_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Charm Medal 01 A",
              "technical": "DA_OI_BackpackCharm_Medal_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Charm Medal 02 A",
              "technical": "DA_OI_BackpackCharm_Medal_02_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Charm Mini Remote 01 A",
              "technical": "DA_OI_BackpackCharm_MiniRemote_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Charm Moon Lander 01 A",
              "technical": "DA_OI_BackpackCharm_MoonLander_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Charm None",
              "technical": "DA_OI_BackpackCharm_None",
              "status": "unassessed"
            },
            {
              "name": "Backpack Charm Origami 01 A",
              "technical": "DA_OI_BackpackCharm_Origami_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Charm Pager 01 A",
              "technical": "DA_OI_BackpackCharm_Pager_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Charm Pink Rabbits Foot",
              "technical": "DA_OI_BackpackCharm_PinkRabbitsFoot",
              "status": "unassessed"
            },
            {
              "name": "Backpack Charm Pocket Radar",
              "technical": "DA_OI_BackpackCharm_PocketRadar",
              "status": "unassessed"
            },
            {
              "name": "Backpack Charm Polaroid Camera 01 A",
              "technical": "DA_OI_BackpackCharm_PolaroidCamera_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Charm Portable Jammer 01 A",
              "technical": "DA_OI_BackpackCharm_PortableJammer_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Charm Pressure Gauge 01 A",
              "technical": "DA_OI_BackpackCharm_PressureGauge_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Charm Rabbit Foot",
              "technical": "DA_OI_BackpackCharm_RabbitFoot",
              "status": "unassessed"
            },
            {
              "name": "Backpack Charm Radio 01 A",
              "technical": "DA_OI_BackpackCharm_Radio_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Charm Remove Before Flight 01 A",
              "technical": "DA_OI_BackpackCharm_RemoveBeforeFlight_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Charm Rubber Duck 01 A",
              "technical": "DA_OI_BackpackCharm_RubberDuck_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Charm Rubber Duck 02 A",
              "technical": "DA_OI_BackpackCharm_RubberDuck_02_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Charm Sample Tube 01 A",
              "technical": "DA_OI_BackpackCharm_SampleTube_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Charm Sextant 01 A",
              "technical": "DA_OI_BackpackCharm_Sextant_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Charm Shears 01 A",
              "technical": "DA_OI_BackpackCharm_Shears_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Charm Sheriff Badge 01 A",
              "technical": "DA_OI_BackpackCharm_SheriffBadge_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Charm Shuttle Key Chain 01 A",
              "technical": "DA_OI_BackpackCharm_ShuttleKeyChain_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Charm Snow Globe 01 A",
              "technical": "DA_OI_BackpackCharm_SnowGlobe_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Charm Soccer Finals Ticket 01 A",
              "technical": "DA_OI_BackpackCharm_SoccerFinalsTicket_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Charm Soda Can 01 A",
              "technical": "DA_OI_BackpackCharm_SodaCan_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Charm Space Blaster",
              "technical": "DA_OI_BackpackCharm_SpaceBlaster",
              "status": "unassessed"
            },
            {
              "name": "Backpack Charm Spotting Scope 01 A",
              "technical": "DA_OI_BackpackCharm_SpottingScope_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Charm Stapler 01 A",
              "technical": "DA_OI_BackpackCharm_Stapler_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Charm Sundial",
              "technical": "DA_OI_BackpackCharm_Sundial",
              "status": "unassessed"
            },
            {
              "name": "Backpack Charm Toy Plane 01 A",
              "technical": "DA_OI_BackpackCharm_ToyPlane_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Charm Toy Robot 01 A",
              "technical": "DA_OI_BackpackCharm_ToyRobot_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Charm Traffic Band 01 A",
              "technical": "DA_OI_BackpackCharm_TrafficBand_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Charm Twig Trinket 01 A",
              "technical": "DA_OI_BackpackCharm_TwigTrinket_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Charm Vintage Mic 01 A",
              "technical": "DA_OI_BackpackCharm_VintageMic_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Charm Vintage Toy Car 01 A",
              "technical": "DA_OI_BackpackCharm_VintageToyCar_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Charm Water Bottle 01 A",
              "technical": "DA_OI_BackpackCharm_WaterBottle_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Charm Whistle Bundle 01 A",
              "technical": "DA_OI_BackpackCharm_WhistleBundle_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Charm Wine Bottle 01 A",
              "technical": "DA_OI_BackpackCharm_WineBottle_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Charm Wire Spool 01 A",
              "technical": "DA_OI_BackpackCharm_WireSpool_01_A",
              "status": "unassessed"
            },
            {
              "name": "Backpack Container Apocalypse Bag",
              "technical": "DA_OI_BackpackContainer_ApocalypseBag",
              "status": "unassessed"
            },
            {
              "name": "Backpack Container Arc Pack",
              "technical": "DA_OI_BackpackContainer_ArcPack",
              "status": "unassessed"
            },
            {
              "name": "Backpack Container Barrel",
              "technical": "DA_OI_BackpackContainer_Barrel",
              "status": "unassessed"
            },
            {
              "name": "Backpack Container Basket Backpack",
              "technical": "DA_OI_BackpackContainer_BasketBackpack",
              "status": "unassessed"
            },
            {
              "name": "Backpack Container Boiler",
              "technical": "DA_OI_BackpackContainer_Boiler",
              "status": "unassessed"
            },
            {
              "name": "Backpack Container Cartographer",
              "technical": "DA_OI_BackpackContainer_Cartographer",
              "status": "unassessed"
            },
            {
              "name": "Backpack Container Duffelbag",
              "technical": "DA_OI_BackpackContainer_Duffelbag",
              "status": "unassessed"
            },
            {
              "name": "Backpack Container Fieldphone",
              "technical": "DA_OI_BackpackContainer_Fieldphone",
              "status": "unassessed"
            },
            {
              "name": "Backpack Container Hardcase",
              "technical": "DA_OI_BackpackContainer_Hardcase",
              "status": "unassessed"
            },
            {
              "name": "Backpack Container Hiking Bag",
              "technical": "DA_OI_BackpackContainer_HikingBag",
              "status": "unassessed"
            },
            {
              "name": "Backpack Container Iron Stove",
              "technical": "DA_OI_BackpackContainer_IronStove",
              "status": "unassessed"
            },
            {
              "name": "Backpack Container Luggage",
              "technical": "DA_OI_BackpackContainer_Luggage",
              "status": "unassessed"
            },
            {
              "name": "Backpack Container Occult",
              "technical": "DA_OI_BackpackContainer_Occult",
              "status": "unassessed"
            },
            {
              "name": "Backpack Container Oxygen Tank",
              "technical": "DA_OI_BackpackContainer_OxygenTank",
              "status": "unassessed"
            },
            {
              "name": "Backpack Container Portmanteau",
              "technical": "DA_OI_BackpackContainer_Portmanteau",
              "status": "unassessed"
            },
            {
              "name": "Backpack Container Rollupbackpack",
              "technical": "DA_OI_BackpackContainer_Rollupbackpack",
              "status": "unassessed"
            },
            {
              "name": "Backpack Container Scout",
              "technical": "DA_OI_BackpackContainer_Scout",
              "status": "unassessed"
            },
            {
              "name": "Backpack Container Seasack",
              "technical": "DA_OI_BackpackContainer_Seasack",
              "status": "unassessed"
            },
            {
              "name": "Backpack Container Souls Offering Bag",
              "technical": "DA_OI_BackpackContainer_SoulsOfferingBag",
              "status": "unassessed"
            },
            {
              "name": "Backpack Container Standard",
              "technical": "DA_OI_BackpackContainer_Standard",
              "status": "unassessed"
            },
            {
              "name": "Backpack Container Survival",
              "technical": "DA_OI_BackpackContainer_Survival",
              "status": "unassessed"
            },
            {
              "name": "Backpack Container Tactical Bag",
              "technical": "DA_OI_BackpackContainer_TacticalBag",
              "status": "unassessed"
            },
            {
              "name": "Backpack Container Tech Bag",
              "technical": "DA_OI_BackpackContainer_TechBag",
              "status": "unassessed"
            },
            {
              "name": "Backpack Container Tennis Bag",
              "technical": "DA_OI_BackpackContainer_TennisBag",
              "status": "unassessed"
            },
            {
              "name": "Backpack Container Toolbox",
              "technical": "DA_OI_BackpackContainer_Toolbox",
              "status": "unassessed"
            },
            {
              "name": "Backpack Container Trash Pile Bag",
              "technical": "DA_OI_BackpackContainer_TrashPileBag",
              "status": "unassessed"
            },
            {
              "name": "Backpack Container Treasure Chest",
              "technical": "DA_OI_BackpackContainer_TreasureChest",
              "status": "unassessed"
            },
            {
              "name": "Backpack Container Treasure Hunter Backpack",
              "technical": "DA_OI_BackpackContainer_TreasureHunterBackpack",
              "status": "unassessed"
            },
            {
              "name": "Backpack Container Trunk",
              "technical": "DA_OI_BackpackContainer_Trunk",
              "status": "unassessed"
            },
            {
              "name": "Backpack Container Tvoltpack",
              "technical": "DA_OI_BackpackContainer_Tvoltpack",
              "status": "unassessed"
            },
            {
              "name": "Backpack Container Urn",
              "technical": "DA_OI_BackpackContainer_Urn",
              "status": "unassessed"
            },
            {
              "name": "Backpack Container Utility",
              "technical": "DA_OI_BackpackContainer_Utility",
              "status": "unassessed"
            },
            {
              "name": "Backpack Container Vessel",
              "technical": "DA_OI_BackpackContainer_Vessel",
              "status": "unassessed"
            },
            {
              "name": "Backpack Container Water Tank",
              "technical": "DA_OI_BackpackContainer_WaterTank",
              "status": "unassessed"
            },
            {
              "name": "Backpack Frame Standard",
              "technical": "DA_OI_BackpackFrame_Standard",
              "status": "unassessed"
            },
            {
              "name": "Body Body A",
              "technical": "DA_OI_Body_BodyA",
              "status": "unassessed"
            },
            {
              "name": "Body Body B",
              "technical": "DA_OI_Body_BodyB",
              "status": "unassessed"
            },
            {
              "name": "Face Face 01 Pio",
              "technical": "DA_OI_Face_Face_01_Pio",
              "status": "unassessed"
            },
            {
              "name": "Face Face 02 Pio",
              "technical": "DA_OI_Face_Face_02_Pio",
              "status": "unassessed"
            },
            {
              "name": "Face Face 03 Pio",
              "technical": "DA_OI_Face_Face_03_Pio",
              "status": "unassessed"
            },
            {
              "name": "Face Face 04 Pio",
              "technical": "DA_OI_Face_Face_04_Pio",
              "status": "unassessed"
            },
            {
              "name": "Face Face 05 Pio",
              "technical": "DA_OI_Face_Face_05_Pio",
              "status": "unassessed"
            },
            {
              "name": "Face Face 06 Pio",
              "technical": "DA_OI_Face_Face_06_Pio",
              "status": "unassessed"
            },
            {
              "name": "Face Face 07 Pio",
              "technical": "DA_OI_Face_Face_07_Pio",
              "status": "unassessed"
            },
            {
              "name": "Face Face 08 Pio",
              "technical": "DA_OI_Face_Face_08_Pio",
              "status": "unassessed"
            },
            {
              "name": "Face Face 09 Pio",
              "technical": "DA_OI_Face_Face_09_Pio",
              "status": "unassessed"
            },
            {
              "name": "Face Face 10 Pio",
              "technical": "DA_OI_Face_Face_10_Pio",
              "status": "unassessed"
            },
            {
              "name": "Face Face 11 Pio",
              "technical": "DA_OI_Face_Face_11_Pio",
              "status": "unassessed"
            },
            {
              "name": "Face Face 12 Pio",
              "technical": "DA_OI_Face_Face_12_Pio",
              "status": "unassessed"
            },
            {
              "name": "Face Face 13 Pio",
              "technical": "DA_OI_Face_Face_13_Pio",
              "status": "unassessed"
            },
            {
              "name": "Face Face 14 Pio",
              "technical": "DA_OI_Face_Face_14_Pio",
              "status": "unassessed"
            },
            {
              "name": "Face Face 15 Pio",
              "technical": "DA_OI_Face_Face_15_Pio",
              "status": "unassessed"
            },
            {
              "name": "Face Face 18 Pio",
              "technical": "DA_OI_Face_Face_18_Pio",
              "status": "unassessed"
            },
            {
              "name": "Face Face 19 Pio",
              "technical": "DA_OI_Face_Face_19_Pio",
              "status": "unassessed"
            },
            {
              "name": "Face Face 20 Pio",
              "technical": "DA_OI_Face_Face_20_Pio",
              "status": "unassessed"
            },
            {
              "name": "Face Style Acne Scar",
              "technical": "DA_OI_FaceStyle_AcneScar",
              "status": "unassessed"
            },
            {
              "name": "Face Style Bandaid Cheek",
              "technical": "DA_OI_FaceStyle_BandaidCheek",
              "status": "unassessed"
            },
            {
              "name": "Face Style Bandaid Nose",
              "technical": "DA_OI_FaceStyle_BandaidNose",
              "status": "unassessed"
            },
            {
              "name": "Face Style Black Eye",
              "technical": "DA_OI_FaceStyle_BlackEye",
              "status": "unassessed"
            },
            {
              "name": "Face Style Bruised",
              "technical": "DA_OI_FaceStyle_Bruised",
              "status": "unassessed"
            },
            {
              "name": "Face Style Burn Scar",
              "technical": "DA_OI_FaceStyle_BurnScar",
              "status": "unassessed"
            },
            {
              "name": "Face Style Dirt Full",
              "technical": "DA_OI_FaceStyle_DirtFull",
              "status": "unassessed"
            },
            {
              "name": "Face Style Dirt Spray",
              "technical": "DA_OI_FaceStyle_DirtSpray",
              "status": "unassessed"
            },
            {
              "name": "Face Style Eye Band",
              "technical": "DA_OI_FaceStyle_EyeBand",
              "status": "unassessed"
            },
            {
              "name": "Face Style Eyebrow Bandaid",
              "technical": "DA_OI_FaceStyle_EyebrowBandaid",
              "status": "unassessed"
            },
            {
              "name": "Face Style Eyeliner",
              "technical": "DA_OI_FaceStyle_Eyeliner",
              "status": "unassessed"
            },
            {
              "name": "Face Style Eye Scar",
              "technical": "DA_OI_FaceStyle_EyeScar",
              "status": "unassessed"
            },
            {
              "name": "Face Style Eye Stripe",
              "technical": "DA_OI_FaceStyle_EyeStripe",
              "status": "unassessed"
            },
            {
              "name": "Face Style Eye Stripe Blue",
              "technical": "DA_OI_FaceStyle_EyeStripeBlue",
              "status": "unassessed"
            },
            {
              "name": "Face Style Eye Stripe Green",
              "technical": "DA_OI_FaceStyle_EyeStripeGreen",
              "status": "unassessed"
            },
            {
              "name": "Face Style Freckles",
              "technical": "DA_OI_FaceStyle_Freckles",
              "status": "unassessed"
            },
            {
              "name": "Face Style Gel Patches",
              "technical": "DA_OI_FaceStyle_GelPatches",
              "status": "unassessed"
            },
            {
              "name": "Face Style Henna Tattoo",
              "technical": "DA_OI_FaceStyle_HennaTattoo",
              "status": "unassessed"
            },
            {
              "name": "Face Style Henna Tattoo Red",
              "technical": "DA_OI_FaceStyle_HennaTattooRed",
              "status": "unassessed"
            },
            {
              "name": "Face Style Kabuki Face",
              "technical": "DA_OI_FaceStyle_KabukiFace",
              "status": "unassessed"
            },
            {
              "name": "Face Style Lip Scar",
              "technical": "DA_OI_FaceStyle_LipScar",
              "status": "unassessed"
            },
            {
              "name": "Face Style Makeup Glam Purple",
              "technical": "DA_OI_FaceStyle_MakeupGlamPurple",
              "status": "unassessed"
            },
            {
              "name": "Face Style Military Paint",
              "technical": "DA_OI_FaceStyle_MilitaryPaint",
              "status": "unassessed"
            },
            {
              "name": "Face Style Military Paint White",
              "technical": "DA_OI_FaceStyle_MilitaryPaintWhite",
              "status": "unassessed"
            },
            {
              "name": "Face Style Neutral Makeup",
              "technical": "DA_OI_FaceStyle_NeutralMakeup",
              "status": "unassessed"
            },
            {
              "name": "Face Style None",
              "technical": "DA_OI_FaceStyle_None",
              "status": "unassessed"
            },
            {
              "name": "Face Style Nose Stripe",
              "technical": "DA_OI_FaceStyle_NoseStripe",
              "status": "unassessed"
            },
            {
              "name": "Face Style Oil Splatter",
              "technical": "DA_OI_FaceStyle_OilSplatter",
              "status": "unassessed"
            },
            {
              "name": "Face Style Painted Mask",
              "technical": "DA_OI_FaceStyle_PaintedMask",
              "status": "unassessed"
            },
            {
              "name": "Face Style Painted Mask Black",
              "technical": "DA_OI_FaceStyle_PaintedMaskBlack",
              "status": "unassessed"
            },
            {
              "name": "Face Style Shark Bite",
              "technical": "DA_OI_FaceStyle_SharkBite",
              "status": "unassessed"
            },
            {
              "name": "Face Style Skull Paint",
              "technical": "DA_OI_FaceStyle_SkullPaint",
              "status": "unassessed"
            },
            {
              "name": "Face Style Smokey Eye",
              "technical": "DA_OI_FaceStyle_SmokeyEye",
              "status": "unassessed"
            },
            {
              "name": "Face Style Smokey Eye Red",
              "technical": "DA_OI_FaceStyle_SmokeyEyeRed",
              "status": "unassessed"
            },
            {
              "name": "Face Style Sot",
              "technical": "DA_OI_FaceStyle_Sot",
              "status": "unassessed"
            },
            {
              "name": "Face Style SOTBlue",
              "technical": "DA_OI_FaceStyle_SOTBlue",
              "status": "unassessed"
            },
            {
              "name": "Face Style Speckled Face Art",
              "technical": "DA_OI_FaceStyle_SpeckledFaceArt",
              "status": "unassessed"
            },
            {
              "name": "Face Style Sport Face Paint",
              "technical": "DA_OI_FaceStyle_SportFacePaint",
              "status": "unassessed"
            },
            {
              "name": "Face Style Sport Face Paint Blue",
              "technical": "DA_OI_FaceStyle_SportFacePaintBlue",
              "status": "unassessed"
            },
            {
              "name": "Face Style Sprayed Eyes",
              "technical": "DA_OI_FaceStyle_SprayedEyes",
              "status": "unassessed"
            },
            {
              "name": "Face Style Sprayed Eyes Red",
              "technical": "DA_OI_FaceStyle_SprayedEyesRed",
              "status": "unassessed"
            },
            {
              "name": "Face Style Tattoo Glitch",
              "technical": "DA_OI_FaceStyle_TattooGlitch",
              "status": "unassessed"
            },
            {
              "name": "Face Style Tattoo Kraken",
              "technical": "DA_OI_FaceStyle_TattooKraken",
              "status": "unassessed"
            },
            {
              "name": "Face Style Tattoo Snake",
              "technical": "DA_OI_FaceStyle_TattooSnake",
              "status": "unassessed"
            },
            {
              "name": "Face Style Tattoo Spider",
              "technical": "DA_OI_FaceStyle_TattooSpider",
              "status": "unassessed"
            },
            {
              "name": "Face Style Tribal Eye",
              "technical": "DA_OI_FaceStyle_TribalEye",
              "status": "unassessed"
            },
            {
              "name": "Face Style Tribal Eye Red",
              "technical": "DA_OI_FaceStyle_TribalEyeRed",
              "status": "unassessed"
            },
            {
              "name": "Face Style Tribal Face",
              "technical": "DA_OI_FaceStyle_TribalFace",
              "status": "unassessed"
            },
            {
              "name": "Face Style Tribal Sheep Face",
              "technical": "DA_OI_FaceStyle_TribalSheepFace",
              "status": "unassessed"
            },
            {
              "name": "Facial Hair Beard Full",
              "technical": "DA_OI_FacialHair_BeardFull",
              "status": "unassessed"
            },
            {
              "name": "Facial Hair Moustache Regular",
              "technical": "DA_OI_FacialHair_MoustacheRegular",
              "status": "unassessed"
            },
            {
              "name": "Facial Hair Mutton Chops",
              "technical": "DA_OI_FacialHair_MuttonChops",
              "status": "unassessed"
            },
            {
              "name": "Facial Hair None",
              "technical": "DA_OI_FacialHair_None",
              "status": "unassessed"
            },
            {
              "name": "Facial Hair Side Burns",
              "technical": "DA_OI_FacialHair_SideBurns",
              "status": "unassessed"
            },
            {
              "name": "Facial Hair Stubble",
              "technical": "DA_OI_FacialHair_Stubble",
              "status": "unassessed"
            },
            {
              "name": "Facial Hair Stubble Short",
              "technical": "DA_OI_FacialHair_StubbleShort",
              "status": "unassessed"
            },
            {
              "name": "Hair Afro Fade",
              "technical": "DA_OI_Hair_AfroFade",
              "status": "unassessed"
            },
            {
              "name": "Hair Afro Long",
              "technical": "DA_OI_Hair_AfroLong",
              "status": "unassessed"
            },
            {
              "name": "Hair Back Slick Messy",
              "technical": "DA_OI_Hair_BackSlickMessy",
              "status": "unassessed"
            },
            {
              "name": "Hair Backslick Short",
              "technical": "DA_OI_Hair_BackslickShort",
              "status": "unassessed"
            },
            {
              "name": "Hair Bald",
              "technical": "DA_OI_Hair_Bald",
              "status": "unassessed"
            },
            {
              "name": "Hair Bob",
              "technical": "DA_OI_Hair_Bob",
              "status": "unassessed"
            },
            {
              "name": "Hair Bob Slick",
              "technical": "DA_OI_Hair_BobSlick",
              "status": "unassessed"
            },
            {
              "name": "Hair Bowl Cut Messy",
              "technical": "DA_OI_Hair_BowlCutMessy",
              "status": "unassessed"
            },
            {
              "name": "Hair Braided Bun",
              "technical": "DA_OI_Hair_BraidedBun",
              "status": "unassessed"
            },
            {
              "name": "Hair Buzzcut",
              "technical": "DA_OI_Hair_Buzzcut",
              "status": "unassessed"
            },
            {
              "name": "Hair Curly Short",
              "technical": "DA_OI_Hair_CurlyShort",
              "status": "unassessed"
            },
            {
              "name": "Hair Curtain Fade",
              "technical": "DA_OI_Hair_CurtainFade",
              "status": "unassessed"
            },
            {
              "name": "Hair Dreads Short",
              "technical": "DA_OI_Hair_DreadsShort",
              "status": "unassessed"
            },
            {
              "name": "Hair Dreads Sidecut",
              "technical": "DA_OI_Hair_DreadsSidecut",
              "status": "unassessed"
            },
            {
              "name": "Hair French Braids",
              "technical": "DA_OI_Hair_FrenchBraids",
              "status": "unassessed"
            },
            {
              "name": "Hair High Ponytail",
              "technical": "DA_OI_Hair_HighPonytail",
              "status": "unassessed"
            },
            {
              "name": "Hair Lob Bangs",
              "technical": "DA_OI_Hair_LobBangs",
              "status": "unassessed"
            },
            {
              "name": "Hair Low Bun",
              "technical": "DA_OI_Hair_LowBun",
              "status": "unassessed"
            },
            {
              "name": "Hair Middle Fade",
              "technical": "DA_OI_Hair_MiddleFade",
              "status": "unassessed"
            },
            {
              "name": "Hair Mohawk Messy",
              "technical": "DA_OI_Hair_MohawkMessy",
              "status": "unassessed"
            },
            {
              "name": "Hair Mullet",
              "technical": "DA_OI_Hair_Mullet",
              "status": "unassessed"
            },
            {
              "name": "Hair Mullet Curly",
              "technical": "DA_OI_Hair_MulletCurly",
              "status": "unassessed"
            },
            {
              "name": "Hair Ponytail Big Curly",
              "technical": "DA_OI_Hair_PonytailBigCurly",
              "status": "unassessed"
            },
            {
              "name": "Hair Recede Messy",
              "technical": "DA_OI_Hair_RecedeMessy",
              "status": "unassessed"
            },
            {
              "name": "Hair Short Upswept",
              "technical": "DA_OI_Hair_ShortUpswept",
              "status": "unassessed"
            },
            {
              "name": "Hair Short Wavy",
              "technical": "DA_OI_Hair_ShortWavy",
              "status": "unassessed"
            },
            {
              "name": "Outfit Abandoned Astronaut",
              "technical": "DA_OI_Outfit_AbandonedAstronaut",
              "status": "unassessed"
            },
            {
              "name": "Outfit Abyss",
              "technical": "DA_OI_Outfit_Abyss",
              "status": "unassessed"
            },
            {
              "name": "Outfit Agile Astronaut",
              "technical": "DA_OI_Outfit_AgileAstronaut",
              "status": "unassessed"
            },
            {
              "name": "Outfit Angler",
              "technical": "DA_OI_Outfit_Angler",
              "status": "unassessed"
            },
            {
              "name": "Outfit Antler Shaman",
              "technical": "DA_OI_Outfit_AntlerShaman",
              "status": "unassessed"
            },
            {
              "name": "Outfit Arc Core",
              "technical": "DA_OI_Outfit_ArcCore",
              "status": "unassessed"
            },
            {
              "name": "Outfit Aviator",
              "technical": "DA_OI_Outfit_Aviator",
              "status": "unassessed"
            },
            {
              "name": "Outfit Banshee",
              "technical": "DA_OI_Outfit_Banshee",
              "status": "unassessed"
            },
            {
              "name": "Outfit Beekeeper",
              "technical": "DA_OI_Outfit_Beekeeper",
              "status": "unassessed"
            },
            {
              "name": "Outfit Bells",
              "technical": "DA_OI_Outfit_Bells",
              "status": "unassessed"
            },
            {
              "name": "Outfit Bomber",
              "technical": "DA_OI_Outfit_Bomber",
              "status": "unassessed"
            },
            {
              "name": "Outfit Buttero",
              "technical": "DA_OI_Outfit_Buttero",
              "status": "unassessed"
            },
            {
              "name": "Outfit Casual Pilot",
              "technical": "DA_OI_Outfit_CasualPilot",
              "status": "unassessed"
            },
            {
              "name": "Outfit Cosmonaut",
              "technical": "DA_OI_Outfit_Cosmonaut",
              "status": "unassessed"
            },
            {
              "name": "Outfit Cowboy Missionary",
              "technical": "DA_OI_Outfit_CowboyMissionary",
              "status": "unassessed"
            },
            {
              "name": "Outfit Dashwood",
              "technical": "DA_OI_Outfit_Dashwood",
              "status": "unassessed"
            },
            {
              "name": "Outfit Deep Sea Diver",
              "technical": "DA_OI_Outfit_DeepSeaDiver",
              "status": "unassessed"
            },
            {
              "name": "Outfit Drafty",
              "technical": "DA_OI_Outfit_Drafty",
              "status": "unassessed"
            },
            {
              "name": "Outfit Driftcoat",
              "technical": "DA_OI_Outfit_Driftcoat",
              "status": "unassessed"
            },
            {
              "name": "Outfit Dweller",
              "technical": "DA_OI_Outfit_Dweller",
              "status": "unassessed"
            },
            {
              "name": "Outfit Enforcer",
              "technical": "DA_OI_Outfit_Enforcer",
              "status": "unassessed"
            },
            {
              "name": "Outfit Expeditioner",
              "technical": "DA_OI_Outfit_Expeditioner",
              "status": "unassessed"
            },
            {
              "name": "Outfit Explorer",
              "technical": "DA_OI_Outfit_Explorer",
              "status": "unassessed"
            },
            {
              "name": "Outfit Fencer",
              "technical": "DA_OI_Outfit_Fencer",
              "status": "unassessed"
            },
            {
              "name": "Outfit Fighter Pilot",
              "technical": "DA_OI_Outfit_FighterPilot",
              "status": "unassessed"
            },
            {
              "name": "Outfit Flamethrower",
              "technical": "DA_OI_Outfit_Flamethrower",
              "status": "unassessed"
            },
            {
              "name": "Outfit Ghillie",
              "technical": "DA_OI_Outfit_Ghillie",
              "status": "unassessed"
            },
            {
              "name": "Outfit Glitch",
              "technical": "DA_OI_Outfit_Glitch",
              "status": "unassessed"
            },
            {
              "name": "Outfit Goalie",
              "technical": "DA_OI_Outfit_Goalie",
              "status": "unassessed"
            },
            {
              "name": "Outfit Grand Racer",
              "technical": "DA_OI_Outfit_GrandRacer",
              "status": "unassessed"
            },
            {
              "name": "Outfit Greenhorn",
              "technical": "DA_OI_Outfit_Greenhorn",
              "status": "unassessed"
            },
            {
              "name": "Outfit Gsuit",
              "technical": "DA_OI_Outfit_Gsuit",
              "status": "unassessed"
            },
            {
              "name": "Outfit GWRanger",
              "technical": "DA_OI_Outfit_GWRanger",
              "status": "unassessed"
            },
            {
              "name": "Outfit Helix",
              "technical": "DA_OI_Outfit_Helix",
              "status": "unassessed"
            },
            {
              "name": "Outfit Hi Tech Hiker",
              "technical": "DA_OI_Outfit_HiTechHiker",
              "status": "unassessed"
            },
            {
              "name": "Outfit Hoplomachus",
              "technical": "DA_OI_Outfit_Hoplomachus",
              "status": "unassessed"
            },
            {
              "name": "Outfit Hornet Hunter",
              "technical": "DA_OI_Outfit_HornetHunter",
              "status": "unassessed"
            },
            {
              "name": "Outfit Hornveil",
              "technical": "DA_OI_Outfit_Hornveil",
              "status": "unassessed"
            },
            {
              "name": "Outfit Huntsman",
              "technical": "DA_OI_Outfit_Huntsman",
              "status": "unassessed"
            },
            {
              "name": "Outfit Hydro Scav",
              "technical": "DA_OI_Outfit_HydroScav",
              "status": "unassessed"
            },
            {
              "name": "Outfit Ironclad",
              "technical": "DA_OI_Outfit_Ironclad",
              "status": "unassessed"
            },
            {
              "name": "Outfit Janitor",
              "technical": "DA_OI_Outfit_Janitor",
              "status": "unassessed"
            },
            {
              "name": "Outfit Jet Pilot",
              "technical": "DA_OI_Outfit_JetPilot",
              "status": "unassessed"
            },
            {
              "name": "Outfit Jukebox",
              "technical": "DA_OI_Outfit_Jukebox",
              "status": "unassessed"
            },
            {
              "name": "Outfit Lemans",
              "technical": "DA_OI_Outfit_Lemans",
              "status": "unassessed"
            },
            {
              "name": "Outfit Loomer",
              "technical": "DA_OI_Outfit_Loomer",
              "status": "unassessed"
            },
            {
              "name": "Outfit Macrame",
              "technical": "DA_OI_Outfit_Macrame",
              "status": "unassessed"
            },
            {
              "name": "Outfit Mask Shift",
              "technical": "DA_OI_Outfit_MaskShift",
              "status": "unassessed"
            },
            {
              "name": "Outfit Matador",
              "technical": "DA_OI_Outfit_Matador",
              "status": "unassessed"
            },
            {
              "name": "Outfit Moonball",
              "technical": "DA_OI_Outfit_Moonball",
              "status": "unassessed"
            },
            {
              "name": "Outfit Moto Racer",
              "technical": "DA_OI_Outfit_MotoRacer",
              "status": "unassessed"
            },
            {
              "name": "Outfit Mountaineer",
              "technical": "DA_OI_Outfit_Mountaineer",
              "status": "unassessed"
            },
            {
              "name": "Outfit Night Runner",
              "technical": "DA_OI_Outfit_NightRunner",
              "status": "unassessed"
            },
            {
              "name": "Outfit Nomad Shroud",
              "technical": "DA_OI_Outfit_NomadShroud",
              "status": "unassessed"
            },
            {
              "name": "Outfit Nova",
              "technical": "DA_OI_Outfit_Nova",
              "status": "unassessed"
            },
            {
              "name": "Outfit Nox",
              "technical": "DA_OI_Outfit_Nox",
              "status": "unassessed"
            },
            {
              "name": "Outfit Nurse",
              "technical": "DA_OI_Outfit_Nurse",
              "status": "unassessed"
            },
            {
              "name": "Outfit Orvax",
              "technical": "DA_OI_Outfit_Orvax",
              "status": "unassessed"
            },
            {
              "name": "Outfit Outlander",
              "technical": "DA_OI_Outfit_Outlander",
              "status": "unassessed"
            },
            {
              "name": "Outfit Patcher",
              "technical": "DA_OI_Outfit_Patcher",
              "status": "unassessed"
            },
            {
              "name": "Outfit Pauldroid",
              "technical": "DA_OI_Outfit_Pauldroid",
              "status": "unassessed"
            },
            {
              "name": "Outfit Picket",
              "technical": "DA_OI_Outfit_Picket",
              "status": "unassessed"
            },
            {
              "name": "Outfit Pirate",
              "technical": "DA_OI_Outfit_Pirate",
              "status": "unassessed"
            },
            {
              "name": "Outfit Popstar",
              "technical": "DA_OI_Outfit_Popstar",
              "status": "unassessed"
            },
            {
              "name": "Outfit Recon",
              "technical": "DA_OI_Outfit_Recon",
              "status": "unassessed"
            },
            {
              "name": "Outfit Recruit",
              "technical": "DA_OI_Outfit_Recruit",
              "status": "unassessed"
            },
            {
              "name": "Outfit Relaxed Astronaut",
              "technical": "DA_OI_Outfit_RelaxedAstronaut",
              "status": "unassessed"
            },
            {
              "name": "Outfit Renzo",
              "technical": "DA_OI_Outfit_Renzo",
              "status": "unassessed"
            },
            {
              "name": "Outfit Ruin Walker",
              "technical": "DA_OI_Outfit_RuinWalker",
              "status": "unassessed"
            },
            {
              "name": "Outfit Scourge Rider",
              "technical": "DA_OI_Outfit_ScourgeRider",
              "status": "unassessed"
            },
            {
              "name": "Outfit Scout",
              "technical": "DA_OI_Outfit_Scout",
              "status": "unassessed"
            },
            {
              "name": "Outfit Shock Warden",
              "technical": "DA_OI_Outfit_ShockWarden",
              "status": "unassessed"
            },
            {
              "name": "Outfit Shroud",
              "technical": "DA_OI_Outfit_Shroud",
              "status": "unassessed"
            },
            {
              "name": "Outfit Shroud Horns",
              "technical": "DA_OI_Outfit_ShroudHorns",
              "status": "unassessed"
            },
            {
              "name": "Outfit Space Chaps",
              "technical": "DA_OI_Outfit_SpaceChaps",
              "status": "unassessed"
            },
            {
              "name": "Outfit Sport",
              "technical": "DA_OI_Outfit_Sport",
              "status": "unassessed"
            },
            {
              "name": "Outfit Station Master",
              "technical": "DA_OI_Outfit_StationMaster",
              "status": "unassessed"
            },
            {
              "name": "Outfit Stealth",
              "technical": "DA_OI_Outfit_Stealth",
              "status": "unassessed"
            },
            {
              "name": "Outfit The Kid",
              "technical": "DA_OI_Outfit_TheKid",
              "status": "unassessed"
            },
            {
              "name": "Outfit Tick Hunter",
              "technical": "DA_OI_Outfit_TickHunter",
              "status": "unassessed"
            },
            {
              "name": "Outfit Vale",
              "technical": "DA_OI_Outfit_Vale",
              "status": "unassessed"
            },
            {
              "name": "Outfit Vanguard",
              "technical": "DA_OI_Outfit_Vanguard",
              "status": "unassessed"
            },
            {
              "name": "Outfit Wasteland Rebel",
              "technical": "DA_OI_Outfit_WastelandRebel",
              "status": "unassessed"
            },
            {
              "name": "Outfit Wayfarer",
              "technical": "DA_OI_Outfit_Wayfarer",
              "status": "unassessed"
            },
            {
              "name": "Outfit Welder",
              "technical": "DA_OI_Outfit_Welder",
              "status": "unassessed"
            },
            {
              "name": "Outfit Wrapper",
              "technical": "DA_OI_Outfit_Wrapper",
              "status": "unassessed"
            },
            {
              "name": "Outfit Yarnface",
              "technical": "DA_OI_Outfit_Yarnface",
              "status": "unassessed"
            },
            {
              "name": "Outfit Yeti",
              "technical": "DA_OI_Outfit_Yeti",
              "status": "unassessed"
            },
            {
              "name": "Raider Tool 10Mil Tool",
              "technical": "DA_OI_RaiderTool_10MilTool",
              "status": "unassessed"
            },
            {
              "name": "Raider Tool Anthropologist Staff Tool",
              "technical": "DA_OI_RaiderTool_AnthropologistStaffTool",
              "status": "unassessed"
            },
            {
              "name": "Raider Tool Arctic Fox",
              "technical": "DA_OI_RaiderTool_ArcticFox",
              "status": "unassessed"
            },
            {
              "name": "Raider Tool Arc Tool",
              "technical": "DA_OI_RaiderTool_ArcTool",
              "status": "unassessed"
            },
            {
              "name": "Raider Tool Bastion Mace",
              "technical": "DA_OI_RaiderTool_BastionMace",
              "status": "unassessed"
            },
            {
              "name": "Raider Tool Default",
              "technical": "DA_OI_RaiderTool_Default",
              "status": "unassessed"
            },
            {
              "name": "Raider Tool Goalie Tool",
              "technical": "DA_OI_RaiderTool_GoalieTool",
              "status": "unassessed"
            },
            {
              "name": "Raider Tool Ice Climber",
              "technical": "DA_OI_RaiderTool_IceClimber",
              "status": "unassessed"
            },
            {
              "name": "Raider Tool Moby Dick",
              "technical": "DA_OI_RaiderTool_MobyDick",
              "status": "unassessed"
            },
            {
              "name": "Raider Tool Reaver Tool",
              "technical": "DA_OI_RaiderTool_ReaverTool",
              "status": "unassessed"
            },
            {
              "name": "Raider Tool Relaxed Austronaut Tool",
              "technical": "DA_OI_RaiderTool_RelaxedAustronautTool",
              "status": "unassessed"
            },
            {
              "name": "Raider Tool Renegade",
              "technical": "DA_OI_RaiderTool_Renegade",
              "status": "unassessed"
            },
            {
              "name": "Raider Tool Riot Sledge Hammer",
              "technical": "DA_OI_RaiderTool_RiotSledgeHammer",
              "status": "unassessed"
            },
            {
              "name": "Raider Tool Scrap Mechanic",
              "technical": "DA_OI_RaiderTool_ScrapMechanic",
              "status": "unassessed"
            },
            {
              "name": "Raider Tool Shroud Horns Tool",
              "technical": "DA_OI_RaiderTool_ShroudHornsTool",
              "status": "unassessed"
            },
            {
              "name": "Raider Tool The Kid Tool",
              "technical": "DA_OI_RaiderTool_TheKidTool",
              "status": "unassessed"
            },
            {
              "name": "Raider Tool Urchin Harvester Tool",
              "technical": "DA_OI_RaiderTool_UrchinHarvesterTool",
              "status": "unassessed"
            },
            {
              "name": "Rooster Body Austria White",
              "technical": "DA_OI_RoosterBody_AustriaWhite",
              "status": "unassessed"
            },
            {
              "name": "Rooster Body Ayam Cemani",
              "technical": "DA_OI_RoosterBody_AyamCemani",
              "status": "unassessed"
            },
            {
              "name": "Rooster Body Buff Rock",
              "technical": "DA_OI_RoosterBody_BuffRock",
              "status": "unassessed"
            },
            {
              "name": "Rooster Body Default",
              "technical": "DA_OI_RoosterBody_Default",
              "status": "unassessed"
            },
            {
              "name": "Rooster Body Speckled Sussex",
              "technical": "DA_OI_RoosterBody_SpeckledSussex",
              "status": "unassessed"
            },
            {
              "name": "Rooster Body Welsummer Red",
              "technical": "DA_OI_RoosterBody_WelsummerRed",
              "status": "unassessed"
            },
            {
              "name": "Rooster Outfit Agile Astronaut",
              "technical": "DA_OI_RoosterOutfit_AgileAstronaut",
              "status": "unassessed"
            },
            {
              "name": "Rooster Outfit Arc Core",
              "technical": "DA_OI_RoosterOutfit_ArcCore",
              "status": "unassessed"
            },
            {
              "name": "Rooster Outfit Aviator Glasses",
              "technical": "DA_OI_RoosterOutfit_AviatorGlasses",
              "status": "unassessed"
            },
            {
              "name": "Rooster Outfit Banshee Helmet",
              "technical": "DA_OI_RoosterOutfit_BansheeHelmet",
              "status": "unassessed"
            },
            {
              "name": "Rooster Outfit Batter",
              "technical": "DA_OI_RoosterOutfit_Batter",
              "status": "unassessed"
            },
            {
              "name": "Rooster Outfit Bomber",
              "technical": "DA_OI_RoosterOutfit_Bomber",
              "status": "unassessed"
            },
            {
              "name": "Rooster Outfit Boonie Hat",
              "technical": "DA_OI_RoosterOutfit_BoonieHat",
              "status": "unassessed"
            },
            {
              "name": "Rooster Outfit Bucket",
              "technical": "DA_OI_RoosterOutfit_Bucket",
              "status": "unassessed"
            },
            {
              "name": "Rooster Outfit Buttero",
              "technical": "DA_OI_RoosterOutfit_Buttero",
              "status": "unassessed"
            },
            {
              "name": "Rooster Outfit Cowboy",
              "technical": "DA_OI_RoosterOutfit_Cowboy",
              "status": "unassessed"
            },
            {
              "name": "Rooster Outfit Diver",
              "technical": "DA_OI_RoosterOutfit_Diver",
              "status": "unassessed"
            },
            {
              "name": "Rooster Outfit Drafty",
              "technical": "DA_OI_RoosterOutfit_Drafty",
              "status": "unassessed"
            },
            {
              "name": "Rooster Outfit Fencer",
              "technical": "DA_OI_RoosterOutfit_Fencer",
              "status": "unassessed"
            },
            {
              "name": "Rooster Outfit Fez",
              "technical": "DA_OI_RoosterOutfit_Fez",
              "status": "unassessed"
            },
            {
              "name": "Rooster Outfit Helix",
              "technical": "DA_OI_RoosterOutfit_Helix",
              "status": "unassessed"
            },
            {
              "name": "Rooster Outfit Hoplomachus",
              "technical": "DA_OI_RoosterOutfit_Hoplomachus",
              "status": "unassessed"
            },
            {
              "name": "Rooster Outfit Hornet Hunter",
              "technical": "DA_OI_RoosterOutfit_HornetHunter",
              "status": "unassessed"
            },
            {
              "name": "Rooster Outfit Ironclad",
              "technical": "DA_OI_RoosterOutfit_Ironclad",
              "status": "unassessed"
            },
            {
              "name": "Rooster Outfit Janitor Cap",
              "technical": "DA_OI_RoosterOutfit_JanitorCap",
              "status": "unassessed"
            },
            {
              "name": "Rooster Outfit Mask Shift",
              "technical": "DA_OI_RoosterOutfit_MaskShift",
              "status": "unassessed"
            },
            {
              "name": "Rooster Outfit None",
              "technical": "DA_OI_RoosterOutfit_None",
              "status": "unassessed"
            },
            {
              "name": "Rooster Outfit Nurse",
              "technical": "DA_OI_RoosterOutfit_Nurse",
              "status": "unassessed"
            },
            {
              "name": "Rooster Outfit Outlander",
              "technical": "DA_OI_RoosterOutfit_Outlander",
              "status": "unassessed"
            },
            {
              "name": "Rooster Outfit Pilot",
              "technical": "DA_OI_RoosterOutfit_Pilot",
              "status": "unassessed"
            },
            {
              "name": "Rooster Outfit Pirate",
              "technical": "DA_OI_RoosterOutfit_Pirate",
              "status": "unassessed"
            },
            {
              "name": "Rooster Outfit Pirate Dark",
              "technical": "DA_OI_RoosterOutfit_PirateDark",
              "status": "unassessed"
            },
            {
              "name": "Rooster Outfit Popstar",
              "technical": "DA_OI_RoosterOutfit_Popstar",
              "status": "unassessed"
            },
            {
              "name": "Rooster Outfit Rain Hat",
              "technical": "DA_OI_RoosterOutfit_RainHat",
              "status": "unassessed"
            },
            {
              "name": "Rooster Outfit Renzo",
              "technical": "DA_OI_RoosterOutfit_Renzo",
              "status": "unassessed"
            },
            {
              "name": "Rooster Outfit Ruin Walker",
              "technical": "DA_OI_RoosterOutfit_RuinWalker",
              "status": "unassessed"
            },
            {
              "name": "Rooster Outfit Station Master Hat",
              "technical": "DA_OI_RoosterOutfit_StationMasterHat",
              "status": "unassessed"
            },
            {
              "name": "Rooster Outfit Turban",
              "technical": "DA_OI_RoosterOutfit_Turban",
              "status": "unassessed"
            },
            {
              "name": "Rooster Outfit Vanguard",
              "technical": "DA_OI_RoosterOutfit_Vanguard",
              "status": "unassessed"
            },
            {
              "name": "Rooster Outfit Yeti",
              "technical": "DA_OI_RoosterOutfit_Yeti",
              "status": "unassessed"
            }
          ]
        },
        {
          "name": "Emotes",
          "children": [
            {
              "name": "Angry",
              "technical": "DA_Emote_Angry",
              "status": "unassessed"
            },
            {
              "name": "Backpack Adjust 02",
              "technical": "DA_Emote_BackpackAdjust_02",
              "status": "unassessed"
            },
            {
              "name": "Beekeeper",
              "technical": "DA_Emote_Beekeeper",
              "status": "unassessed"
            },
            {
              "name": "Belt Dance",
              "technical": "DA_Emote_BeltDance",
              "status": "unassessed"
            },
            {
              "name": "Boo",
              "technical": "DA_Emote_Boo",
              "status": "unassessed"
            },
            {
              "name": "Bow 04",
              "technical": "DA_Emote_Bow_04",
              "status": "unassessed"
            },
            {
              "name": "Bow And Arrow 00",
              "technical": "DA_Emote_BowAndArrow_00",
              "status": "unassessed"
            },
            {
              "name": "Camera Heart",
              "technical": "DA_Emote_Camera_Heart",
              "status": "unassessed"
            },
            {
              "name": "Camera Peace",
              "technical": "DA_Emote_Camera_Peace",
              "status": "unassessed"
            },
            {
              "name": "Camera Wave",
              "technical": "DA_Emote_Camera_Wave",
              "status": "unassessed"
            },
            {
              "name": "Cant Hear You 00",
              "technical": "DA_Emote_CantHearYou_00",
              "status": "unassessed"
            },
            {
              "name": "Cheer 00",
              "technical": "DA_Emote_Cheer_00",
              "status": "unassessed"
            },
            {
              "name": "Clap Prayer",
              "technical": "DA_Emote_ClapPrayer",
              "status": "unassessed"
            },
            {
              "name": "Come Here Boy 00",
              "technical": "DA_Emote_ComeHereBoy_00",
              "status": "unassessed"
            },
            {
              "name": "Come On",
              "technical": "DA_Emote_ComeOn",
              "status": "unassessed"
            },
            {
              "name": "Compass Check",
              "technical": "DA_Emote_CompassCheck",
              "status": "unassessed"
            },
            {
              "name": "Cowboy",
              "technical": "DA_Emote_Cowboy",
              "status": "unassessed"
            },
            {
              "name": "Dance",
              "technical": "DA_Emote_Dance",
              "status": "unassessed"
            },
            {
              "name": "Dance 01",
              "technical": "DA_Emote_Dance_01",
              "status": "unassessed"
            },
            {
              "name": "Dance Pumped",
              "technical": "DA_Emote_DancePumped",
              "status": "unassessed"
            },
            {
              "name": "Deflated 00",
              "technical": "DA_Emote_Deflated_00",
              "status": "unassessed"
            },
            {
              "name": "Diving Bell",
              "technical": "DA_Emote_DivingBell",
              "status": "unassessed"
            },
            {
              "name": "Dont Shoot",
              "technical": "DA_Emote_DontShoot",
              "status": "unassessed"
            },
            {
              "name": "Finger Gun Duel 00",
              "technical": "DA_Emote_FingerGunDuel_00",
              "status": "unassessed"
            },
            {
              "name": "Finger Gun Duel 01",
              "technical": "DA_Emote_FingerGunDuel_01",
              "status": "unassessed"
            },
            {
              "name": "Finger Guns Wave",
              "technical": "DA_Emote_FingerGunsWave",
              "status": "unassessed"
            },
            {
              "name": "Fist In Air",
              "technical": "DA_Emote_FistInAir",
              "status": "unassessed"
            },
            {
              "name": "Fist Shakes 00",
              "technical": "DA_Emote_FistShakes_00",
              "status": "unassessed"
            },
            {
              "name": "Fling Coin 00",
              "technical": "DA_Emote_FlingCoin_00",
              "status": "unassessed"
            },
            {
              "name": "Flirty Greeting",
              "technical": "DA_Emote_FlirtyGreeting",
              "status": "unassessed"
            },
            {
              "name": "Greet",
              "technical": "DA_Emote_Greet",
              "status": "unassessed"
            },
            {
              "name": "Hit Chest 00",
              "technical": "DA_Emote_HitChest_00",
              "status": "unassessed"
            },
            {
              "name": "Howdy",
              "technical": "DA_Emote_Howdy",
              "status": "unassessed"
            },
            {
              "name": "Howl 00",
              "technical": "DA_Emote_Howl_00",
              "status": "unassessed"
            },
            {
              "name": "Huntsman 00",
              "technical": "DA_Emote_Huntsman_00",
              "status": "unassessed"
            },
            {
              "name": "ISee You",
              "technical": "DA_Emote_ISeeYou",
              "status": "unassessed"
            },
            {
              "name": "Just Stop",
              "technical": "DA_Emote_JustStop",
              "status": "unassessed"
            },
            {
              "name": "Locomotive",
              "technical": "DA_Emote_Locomotive",
              "status": "unassessed"
            },
            {
              "name": "Metal",
              "technical": "DA_Emote_Metal",
              "status": "unassessed"
            },
            {
              "name": "Mock Scared",
              "technical": "DA_Emote_MockScared",
              "status": "unassessed"
            },
            {
              "name": "No",
              "technical": "DA_Emote_No",
              "status": "unassessed"
            },
            {
              "name": "Nonsensical Hand Signals",
              "technical": "DA_Emote_NonsensicalHandSignals",
              "status": "unassessed"
            },
            {
              "name": "Pantomime",
              "technical": "DA_Emote_Pantomime",
              "status": "unassessed"
            },
            {
              "name": "Peace",
              "technical": "DA_Emote_Peace",
              "status": "unassessed"
            },
            {
              "name": "Plant Small Flag",
              "technical": "DA_Emote_PlantSmallFlag",
              "status": "unassessed"
            },
            {
              "name": "Power Pose 00",
              "technical": "DA_Emote_PowerPose_00",
              "status": "unassessed"
            },
            {
              "name": "Punch Hands",
              "technical": "DA_Emote_PunchHands",
              "status": "unassessed"
            },
            {
              "name": "Reload Arms 00",
              "technical": "DA_Emote_ReloadArms_00",
              "status": "unassessed"
            },
            {
              "name": "Right Here 00",
              "technical": "DA_Emote_RightHere_00",
              "status": "unassessed"
            },
            {
              "name": "Right On",
              "technical": "DA_Emote_RightOn",
              "status": "unassessed"
            },
            {
              "name": "River Dance",
              "technical": "DA_Emote_RiverDance",
              "status": "unassessed"
            },
            {
              "name": "Salute",
              "technical": "DA_Emote_Salute",
              "status": "unassessed"
            },
            {
              "name": "Scout Ahead 01",
              "technical": "DA_Emote_ScoutAhead_01",
              "status": "unassessed"
            },
            {
              "name": "Slit Throat 00",
              "technical": "DA_Emote_SlitThroat_00",
              "status": "unassessed"
            },
            {
              "name": "Slow Dance",
              "technical": "DA_Emote_SlowDance",
              "status": "unassessed"
            },
            {
              "name": "Smelly",
              "technical": "DA_Emote_Smelly",
              "status": "unassessed"
            },
            {
              "name": "Smelly Armpit",
              "technical": "DA_Emote_SmellyArmpit",
              "status": "unassessed"
            },
            {
              "name": "Sorry",
              "technical": "DA_Emote_Sorry",
              "status": "unassessed"
            },
            {
              "name": "Spit In Hands 00",
              "technical": "DA_Emote_SpitInHands_00",
              "status": "unassessed"
            },
            {
              "name": "Squat",
              "technical": "DA_Emote_Squat",
              "status": "unassessed"
            },
            {
              "name": "Stretching 01",
              "technical": "DA_Emote_Stretching_01",
              "status": "unassessed"
            },
            {
              "name": "Stretching 02",
              "technical": "DA_Emote_Stretching_02",
              "status": "unassessed"
            },
            {
              "name": "Team Up",
              "technical": "DA_Emote_TeamUp",
              "status": "unassessed"
            },
            {
              "name": "Thank",
              "technical": "DA_Emote_Thank",
              "status": "unassessed"
            },
            {
              "name": "Think About It 00",
              "technical": "DA_Emote_ThinkAboutIt_00",
              "status": "unassessed"
            },
            {
              "name": "Toodeloo",
              "technical": "DA_Emote_Toodeloo",
              "status": "unassessed"
            },
            {
              "name": "Tug Glove 00",
              "technical": "DA_Emote_TugGlove_00",
              "status": "unassessed"
            },
            {
              "name": "Warm Up 00",
              "technical": "DA_Emote_WarmUp_00",
              "status": "unassessed"
            },
            {
              "name": "Water Bottle Empty 00",
              "technical": "DA_Emote_WaterBottleEmpty_00",
              "status": "unassessed"
            },
            {
              "name": "Yes",
              "technical": "DA_Emote_Yes",
              "status": "unassessed"
            },
            {
              "name": "You Come At Me 00",
              "technical": "DA_Emote_YouComeAtMe_00",
              "status": "unassessed"
            }
          ]
        }
      ]
    }
  ]
};
