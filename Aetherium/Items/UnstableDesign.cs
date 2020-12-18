﻿using Aetherium.Utils;
using BepInEx.Configuration;
using R2API;
using R2API.Networking;
using R2API.Networking.Interfaces;
using RoR2;
using RoR2.CharacterAI;
using RoR2.Skills;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using static Aetherium.Utils.MathHelpers;

namespace Aetherium.Items
{
    public class UnstableDesign : ItemBase<UnstableDesign>
    {
        public static ConfigEntry<float> LunarChimeraResummonCooldownDuration;
        public static ConfigEntry<float> LunarChimeraRetargetingCooldown;
        public static ConfigEntry<int> LunarChimeraBaseDamageBoost;
        public static ConfigEntry<int> LunarChimeraAdditionalDamageBoost;
        public static ConfigEntry<int> LunarChimeraBaseHPBoost;
        public static ConfigEntry<int> LunarChimeraBaseAttackSpeedBoost;
        public static ConfigEntry<int> LunarChimeraBaseMovementSpeedBoost;

        public override string ItemName => "Unstable Design";

        public override string ItemLangTokenName => "UNSTABLE_DESIGN";

        public override string ItemPickupDesc => "Every 30 seconds you are compelled to create a very <color=#FF0000>'FRIENDLY'</color> Lunar Chimera, if one of your creations does not already exist.";

        public override string ItemFullDescription => $"Every {LunarChimeraResummonCooldownDuration.Value} seconds you are compelled to create a very <color=#FF0000>'FRIENDLY'</color> Lunar Chimera, if one of your creations does not already exist. " +
            $"\nIt has a <style=cIsDamage>{FloatToPercentageString(LunarChimeraBaseDamageBoost.Value * 10, 1)} base damage boost</style> <style=cStack>(+{FloatToPercentageString(LunarChimeraAdditionalDamageBoost.Value * 10, 1)} per stack)</style>." +
            $"\nIt has a <style=cIsHealing>{FloatToPercentageString(LunarChimeraBaseHPBoost.Value * 10, 1)} base HP boost</style> <style=cStack>(+{FloatToPercentageString(LunarChimeraBaseHPBoost.Value * 10, 1)} per stack)</style>." +
            $"\nIt has a <style=cIsDamage>{FloatToPercentageString(LunarChimeraBaseAttackSpeedBoost.Value * 10, 1)} base attack speed boost</style>." +
            $"\nFinally, it has a <style=cIsUtility>{FloatToPercentageString(LunarChimeraBaseMovementSpeedBoost.Value * 14, 1)} base movement speed boost</style> <style=cStack>(+{FloatToPercentageString(LunarChimeraBaseMovementSpeedBoost.Value * 14, 1)} per stack)</style>." +
            "\nThis monstrosity <style=cIsDamage>can level up from kills</style>.";

        public override string ItemLore => "We entered this predicament when one of our field testers brought back a blueprint from a whole mountain of them they found on the moon. " +
            "The blueprints seemed to have various formulas and pictures on it relating to the weird constructs we saw roaming the place. " +
            "Jimenez from Engineering got his hands on it and thought he could contribute to the rest of the team by deciphering it and creating the contents for us. " +
            "We are now waiting for Security to handle the very <color=#FF0000>'FRIENDLY'</color> construct that is making a mess of the lower sectors of the station. " +
            "Thanks Jimenez.";

        public override ItemTier Tier => ItemTier.Lunar;
        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Cleansable };

        public override string ItemModelPath => "@Aetherium:Assets/Models/Prefabs/Item/UnstableDesign/UnstableDesign.prefab";
        public override string ItemIconPath => "@Aetherium:Assets/Textures/Icons/Item/UnstableDesignIcon.png";

        public static GameObject ItemBodyModelPrefab;
        public static SpawnCard LunarChimeraSpawnCard;
        public static GameObject LunarChimeraMasterPrefab;
        public static GameObject LunarChimeraBodyPrefab;

        public static SkillDef airSkill
        {
            get
            {
                if (!_airSkill) _airSkill = SkillCatalog.GetSkillDef(SkillCatalog.FindSkillIndexByName("SprintShootShards"));
                return _airSkill;
            }
        }

        private static readonly string nameSuffix = "UnstableDesign(Aetherium)";
        private static SkillDef _airSkill = null;

        public override void Init(ConfigFile config)
        {
            CreateConfig(config);
            CreateLang();
            CreateSpawncard();
            CreateItem();
            Hooks();
        }

        private void CreateConfig(ConfigFile config)
        {
            LunarChimeraResummonCooldownDuration = config.Bind<float>("Item: " + ItemName, "Duration of Chimera Resummoning Cooldown", 30f, "What should be our duration between summoning the Lunar Chimera?");
            LunarChimeraRetargetingCooldown = config.Bind<float>("Item: " + ItemName, "Duration of Chimera Retargeting Cooldown", 10f, "If the Lunar Chimera has lost line of sight, what should the cooldown be between checking for targets?");
            LunarChimeraBaseDamageBoost = config.Bind<int>("Item: " + ItemName, "Base Damage Boosting Item Amount", 40, "What should the Lunar Chimera's base damage boost be? (Default: 40 (400% damage boost). This is how many damage boosting items we give it, which give it a 10% damage boost each. Whole numbers only. First stack.)");
            LunarChimeraAdditionalDamageBoost = config.Bind<int>("Item: " + ItemName, "Additional Damage Boosting Item Amount", 10, "What should the Lunar Chimera's additional damage boost be per stack? (Default: 10 (100% damage boost). This is how many damage boosting items we give it, which give it a 10% damage boost each. Whole numbers only.)");
            LunarChimeraBaseHPBoost = config.Bind<int>("Item: " + ItemName, "HP Boosting Item Amount", 10, "What should the Lunar Chimera's base HP boost be? (Default: 10 (100% HP boost). This is how many HP Boost items we give it, which give it a 10% HP boost each. Whole numbers only.)");
            LunarChimeraBaseAttackSpeedBoost = config.Bind<int>("Item: " + ItemName, "Attack Speed Item Amount", 30, "What should the Lunar Chimera's base attack speed boost be? (Default: 30 (300% attack speed boost). This is how many attack speed boost items we give it, which give it a 10% attack speed boost each. Whole numbers only.)");
            LunarChimeraBaseMovementSpeedBoost = config.Bind<int>("Item: " + ItemName, "Movement Speed Item Amount", 2, "What should the Lunar Chimera's base movement speed boost be? (Default: 2 (28% movement speed boost). This is how many goat hooves we give it, which give it a 14% movement speed boost each. Whole numbers only.)");
        }

        private void CreateSpawncard()
        {
            LunarChimeraSpawnCard = Resources.Load<SpawnCard>("SpawnCards/CharacterSpawnCards/cscLunarGolem");
            LunarChimeraSpawnCard = UnityEngine.Object.Instantiate(LunarChimeraSpawnCard);
            LunarChimeraMasterPrefab = LunarChimeraSpawnCard.prefab;
            LunarChimeraMasterPrefab = LunarChimeraMasterPrefab.InstantiateClone($"{LunarChimeraMasterPrefab.name}{nameSuffix}");
            CharacterMaster masterPrefab = LunarChimeraMasterPrefab.GetComponent<CharacterMaster>();
            LunarChimeraBodyPrefab = masterPrefab.bodyPrefab;
            LunarChimeraBodyPrefab = LunarChimeraBodyPrefab.InstantiateClone($"{LunarChimeraBodyPrefab.name}{nameSuffix}");
            masterPrefab.bodyPrefab = LunarChimeraBodyPrefab;
            LunarChimeraSpawnCard.prefab = LunarChimeraMasterPrefab;
            MasterCatalog.getAdditionalEntries += list => list.Add(LunarChimeraMasterPrefab);
            BodyCatalog.getAdditionalEntries += list => list.Add(LunarChimeraBodyPrefab);
            NetworkingAPI.RegisterMessageType<AssignOwner>();
        }

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            ItemBodyModelPrefab = Resources.Load<GameObject>("@Aetherium:Assets/Models/Prefabs/Item/UnstableDesign/UnstableDesignRolledUp.prefab");
            ItemBodyModelPrefab.AddComponent<RoR2.ItemDisplay>();
            ItemBodyModelPrefab.GetComponent<RoR2.ItemDisplay>().rendererInfos = ItemHelpers.ItemDisplaySetup(ItemBodyModelPrefab);

            ItemDisplayRuleDict rules = new ItemDisplayRuleDict(new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0, 0.5f, -0.2f),
                    localAngles = new Vector3(0, 45, 0),
                    localScale = new Vector3(1, 1, 1)
                }
            });
            rules.Add("mdlHuntress", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0, 0, -0.07f),
                    localAngles = new Vector3(0, 45, 0),
                    localScale = new Vector3(1, 1, 1)
                }
            });
            rules.Add("mdlToolbot", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0, 0.8f, -2.2f),
                    localAngles = new Vector3(0, 45, 0),
                    localScale = new Vector3(7, 7, 7)
                }
            });
            rules.Add("mdlEngi", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0, 0.6f, -0.2f),
                    localAngles = new Vector3(0, 45, 0),
                    localScale = new Vector3(1, 1, 1)
                }
            });
            rules.Add("mdlMage", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0, 0.34f, -0.1f),
                    localAngles = new Vector3(0, 45, 0),
                    localScale = new Vector3(1, 1, 1)
                }
            });
            rules.Add("mdlMerc", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0, 0, -0.23f),
                    localAngles = new Vector3(0, 45, 0),
                    localScale = new Vector3(1, 1, 1)
                }
            });
            rules.Add("mdlTreebot", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0, 0.9f, -0.8f),
                    localAngles = new Vector3(0, 45, 0),
                    localScale = new Vector3(3, 3, 3)
                }
            });
            rules.Add("mdlLoader", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0, 0.45f, -0.4f),
                    localAngles = new Vector3(0, 45, 0),
                    localScale = new Vector3(1, 1, 1)
                }
            });
            rules.Add("mdlCroco", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0, 2, 5),
                    localAngles = new Vector3(0, 45, 0),
                    localScale = new Vector3(8, 8, 8)
                }
            });
            rules.Add("mdlCaptain", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0, -0.18f, -0.28f),
                    localAngles = new Vector3(0, 45, 0),
                    localScale = new Vector3(1, 1, 1)
                }
            });
            return rules;
        }

        public override void Hooks()
        {
            On.RoR2.CharacterBody.FixedUpdate += SummonLunarChimera;
            On.RoR2.MapZone.TryZoneStart += LunarChimeraFall;
        }

        private void SummonLunarChimera(On.RoR2.CharacterBody.orig_FixedUpdate orig, CharacterBody self)
        {
            int inventoryCount = GetCount(self);
            CharacterMaster master = self.master;
            if (NetworkServer.active && inventoryCount > 0 && master && !IsMinion(master)) //Check if we're a minion or not. If we are, we don't summon a chimera.
            {
                LunarChimeraComponent lcComponent = LunarChimeraComponent.GetOrCreateComponent(master);
                if (!lcComponent.LastChimeraSpawned || !lcComponent.LastChimeraSpawned.master || !lcComponent.LastChimeraSpawned.master.hasBody)
                {
                    lcComponent.LastChimeraSpawned = null;
                    lcComponent.ResummonCooldown -= Time.fixedDeltaTime;
                    if (lcComponent.ResummonCooldown <= 0f && SceneCatalog.mostRecentSceneDef != SceneCatalog.GetSceneDefFromSceneName("bazaar"))
                    {
                        DirectorPlacementRule placeRule = new DirectorPlacementRule
                        {
                            placementMode = DirectorPlacementRule.PlacementMode.Approximate,
                            minDistance = 10f,
                            maxDistance = 40f,
                            spawnOnTarget = self.transform
                        };
                        DirectorSpawnRequest directorSpawnRequest = new DirectorSpawnRequest(LunarChimeraSpawnCard, placeRule, RoR2Application.rng)
                        {
                            teamIndexOverride = TeamIndex.Player
                            //summonerBodyObject = self.gameObject
                        };
                        GameObject gameObject = DirectorCore.instance.TrySpawnObject(directorSpawnRequest);
                        if (gameObject)
                        {
                            CharacterMaster cMaster = gameObject.GetComponent<CharacterMaster>();
                            if (cMaster)
                            {
                                //RoR2.Chat.AddMessage($"Character Master Found: {component}");
                                cMaster.teamIndex = TeamIndex.Neutral;
                                cMaster.inventory.GiveItem(ItemIndex.BoostDamage, LunarChimeraBaseDamageBoost.Value + (LunarChimeraAdditionalDamageBoost.Value * inventoryCount - 1));
                                cMaster.inventory.GiveItem(ItemIndex.BoostHp, LunarChimeraBaseHPBoost.Value * inventoryCount);
                                cMaster.inventory.GiveItem(ItemIndex.BoostAttackSpeed, LunarChimeraBaseAttackSpeedBoost.Value);
                                cMaster.inventory.GiveItem(ItemIndex.Hoof, LunarChimeraBaseMovementSpeedBoost.Value * inventoryCount);
                                cMaster.minionOwnership.SetOwner(master);

                                CharacterBody cBody = cMaster.GetBody();
                                if (cBody)
                                {
                                    //RoR2.Chat.AddMessage($"CharacterBody Found: {component4}");
                                    cBody.teamComponent.teamIndex = TeamIndex.Neutral;
                                    cBody.gameObject.AddComponent<LunarChimeraRetargetComponent>();
                                    lcComponent.LastChimeraSpawned = cBody;
                                    DeathRewards deathRewards = cBody.GetComponent<DeathRewards>();
                                    if (deathRewards)
                                    {
                                        //RoR2.Chat.AddMessage($"DeathRewards Found: {component5}");
                                        deathRewards.goldReward = 0;
                                        deathRewards.expReward = 0;
                                    }
                                    NetworkIdentity bodyNet = cBody.GetComponent<NetworkIdentity>();
                                    if (bodyNet)
                                    {
                                        new AssignOwner(lcComponent.netId, bodyNet.netId).Send(NetworkDestination.Clients);
                                    }
                                }
                            }
                            lcComponent.ResummonCooldown = LunarChimeraResummonCooldownDuration.Value;
                        }
                    }
                }
            }
            orig(self);
        }

        private void LunarChimeraFall(On.RoR2.MapZone.orig_TryZoneStart orig, MapZone self, Collider other)
        {
            if (IsUnstableDesignChimera(other.gameObject))
            {
                CharacterBody body = other.GetComponent<CharacterBody>();
                if (body)
                {
                    var teamComponent = body.teamComponent;
                    teamComponent.teamIndex = TeamIndex.Player; //Set the team of it to player to avoid it dying when it falls into a hellzone.
                    orig(self, other); //Run the effect of whatever zone it is in on it. Since it is of the Player team, it obviously gets teleported back into the zone.
                    teamComponent.teamIndex = TeamIndex.Neutral; //Now make it hostile again. Thanks Obama.
                    return;
                }
            }
            orig(self, other);
        }

        private bool IsMinion(CharacterMaster master)
        {
            // Replace the old minion checker so that it can support enemies that get lunar items too
            return master.minionOwnership &&
                   master.minionOwnership.ownerMaster;
        }

        private bool IsUnstableDesignChimera(GameObject obj) => obj.name.Contains(nameSuffix);

        public class LunarChimeraComponent : MonoBehaviour
        {
            public CharacterBody LastChimeraSpawned;
            public float ResummonCooldown = 0f;
            public Queue<NetworkInstanceId> syncIds = new Queue<NetworkInstanceId>();
            public NetworkInstanceId netId;
            public CharacterMaster master;

            private void Awake()
            {
                master = gameObject.GetComponent<CharacterMaster>();
                netId = gameObject.GetComponent<NetworkIdentity>().netId;
            }

            private void FixedUpdate()
            {
                if (syncIds.Count > 0)
                {
                    NetworkInstanceId syncId = syncIds.Dequeue();
                    GameObject supposedChimera = Util.FindNetworkObject(syncId);
                    if (supposedChimera)
                    {
                        LastChimeraSpawned = supposedChimera.GetComponent<CharacterBody>();
                        CharacterMaster cMaster = LastChimeraSpawned.master;
                        cMaster.minionOwnership.ownerMasterId = netId;
                        MinionOwnership.MinionGroup.SetMinionOwner(cMaster.minionOwnership, netId);
                    }
                    else
                    {
                        syncIds.Enqueue(syncId);
                    }
                }
            }

            public static LunarChimeraComponent GetOrCreateComponent(CharacterMaster master)
            {
                return GetOrCreateComponent(master.gameObject);
            }

            public static LunarChimeraComponent GetOrCreateComponent(GameObject masterObject)
            {
                LunarChimeraComponent thisComponent = masterObject.GetComponent<LunarChimeraComponent>();
                if (!thisComponent) thisComponent = masterObject.AddComponent<LunarChimeraComponent>();
                return thisComponent;
            }
        }

        public class LunarChimeraRetargetComponent : MonoBehaviour
        {
            // make public if you want it to be viewable in RuntimeInspector
            private float retargetTimer = 0f;

            private CharacterMaster master;
            private CharacterBody body;

            private void Awake()
            {
                body = gameObject.GetComponent<CharacterBody>();
                if (body)
                {
                    master = body.master;
                }
                SetCooldown();
            }

            private void FixedUpdate()
            {
                if (master)
                {
                    BaseAI baseAIComponent = master.GetComponent<BaseAI>();
                    if (baseAIComponent)
                    {
                        SkillLocator skillComponent = gameObject.GetComponent<SkillLocator>();
                        if (skillComponent)
                        {
                            CharacterBody targetBody = baseAIComponent.currentEnemy.characterBody;
                            if (targetBody && (!targetBody.characterMotor || !targetBody.characterMotor.isGrounded))
                            {
                                skillComponent.primary.SetSkillOverride(body, airSkill, GenericSkill.SkillOverridePriority.Replacement);
                            }
                            else
                            {
                                skillComponent.primary.UnsetSkillOverride(body, airSkill, GenericSkill.SkillOverridePriority.Replacement);
                            }
                        }
                        retargetTimer -= Time.fixedDeltaTime;
                        if (retargetTimer <= 0)
                        {
                            if (!baseAIComponent.currentEnemy.hasLoS)
                            {
                                baseAIComponent.currentEnemy.Reset();
                                baseAIComponent.ForceAcquireNearestEnemyIfNoCurrentEnemy();
                                SetCooldown();
                            }
                        }
                    }
                }
            }

            private void SetCooldown(float? customCooldown = null)
            {
                if (customCooldown == null) retargetTimer = LunarChimeraRetargetingCooldown.Value;
                else retargetTimer = (float)customCooldown;
            }
        }

        public class AssignOwner : INetMessage
        {
            private NetworkInstanceId ownerNetId;
            private NetworkInstanceId minionNetId;

            public AssignOwner()
            {
            }

            public AssignOwner(NetworkInstanceId ownerNetId, NetworkInstanceId minionNetId)
            {
                this.ownerNetId = ownerNetId;
                this.minionNetId = minionNetId;
            }

            public void Deserialize(NetworkReader reader)
            {
                ownerNetId = reader.ReadNetworkId();
                minionNetId = reader.ReadNetworkId();
            }

            public void OnReceived()
            {
                if (NetworkServer.active) return;
                GameObject owner = Util.FindNetworkObject(ownerNetId);
                if (!owner) return;

                LunarChimeraComponent lcComponent = LunarChimeraComponent.GetOrCreateComponent(owner);
                lcComponent.syncIds.Enqueue(minionNetId);
            }

            public void Serialize(NetworkWriter writer)
            {
                writer.Write(ownerNetId);
                writer.Write(minionNetId);
            }
        }
    }
}