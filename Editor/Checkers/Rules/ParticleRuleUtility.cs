using System.Collections.Generic;
using UnityEngine;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 为粒子评分规则提供只读的 Prefab 粒子系统统计能力。
    /// </summary>
    internal static class ParticleRuleUtility
    {
        /// <summary>
        /// 获取 Prefab 根节点及其未激活子节点中的所有粒子系统。
        /// </summary>
        internal static ParticleSystem[] GetParticleSystems(GameObject prefab)
        {
            return prefab == null
                ? new ParticleSystem[0]
                : prefab.GetComponentsInChildren<ParticleSystem>(true);
        }

        /// <summary>
        /// 获取 Prefab 根节点及其未激活子节点中的所有粒子渲染器。
        /// </summary>
        internal static ParticleSystemRenderer[] GetParticleSystemRenderers(
            GameObject prefab)
        {
            return prefab == null
                ? new ParticleSystemRenderer[0]
                : prefab.GetComponentsInChildren<ParticleSystemRenderer>(true);
        }

        /// <summary>
        /// 计算所有粒子系统的 Max Particles 总量。
        /// </summary>
        internal static int GetMaxParticlesTotal(GameObject prefab)
        {
            ParticleSystem[] systems = GetParticleSystems(prefab);
            int total = 0;

            for (int i = 0; i < systems.Length; i++)
            {
                if (systems[i] != null)
                {
                    total += systems[i].main.maxParticles;
                }
            }

            return total;
        }

        /// <summary>
        /// 统计粒子渲染器使用的不同共享材质数量。
        /// </summary>
        internal static int GetUniqueMaterialCount(GameObject prefab)
        {
            ParticleSystemRenderer[] renderers =
                GetParticleSystemRenderers(prefab);
            HashSet<Material> materials = new HashSet<Material>();

            for (int i = 0; i < renderers.Length; i++)
            {
                ParticleSystemRenderer renderer = renderers[i];

                if (renderer == null)
                {
                    continue;
                }

                Material[] sharedMaterials = renderer.sharedMaterials;

                for (int j = 0; j < sharedMaterials.Length; j++)
                {
                    if (sharedMaterials[j] != null)
                    {
                        materials.Add(sharedMaterials[j]);
                    }
                }
            }

            return materials.Count;
        }

        /// <summary>
        /// 判断任一粒子系统是否启用了 Collision 模块。
        /// </summary>
        internal static bool HasCollisionEnabled(GameObject prefab)
        {
            ParticleSystem[] systems = GetParticleSystems(prefab);

            for (int i = 0; i < systems.Length; i++)
            {
                if (systems[i] != null && systems[i].collision.enabled)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 判断任一粒子系统是否启用了 Lights 模块。
        /// </summary>
        internal static bool HasLightsEnabled(GameObject prefab)
        {
            ParticleSystem[] systems = GetParticleSystems(prefab);

            for (int i = 0; i < systems.Length; i++)
            {
                if (systems[i] != null && systems[i].lights.enabled)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 判断任一粒子系统是否启用了 Trails 模块。
        /// </summary>
        internal static bool HasTrailsEnabled(GameObject prefab)
        {
            ParticleSystem[] systems = GetParticleSystems(prefab);

            for (int i = 0; i < systems.Length; i++)
            {
                if (systems[i] != null && systems[i].trails.enabled)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 判断任一粒子系统是否启用了 Prewarm。
        /// </summary>
        internal static bool HasPrewarmEnabled(GameObject prefab)
        {
            ParticleSystem[] systems = GetParticleSystems(prefab);

            for (int i = 0; i < systems.Length; i++)
            {
                if (systems[i] != null && systems[i].main.prewarm)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 获取所有粒子系统中最长的持续时间。
        /// </summary>
        internal static float GetMaxDuration(GameObject prefab)
        {
            ParticleSystem[] systems = GetParticleSystems(prefab);
            float maxDuration = 0f;

            for (int i = 0; i < systems.Length; i++)
            {
                if (systems[i] == null)
                {
                    continue;
                }

                float duration = systems[i].main.duration;

                if (duration > maxDuration)
                {
                    maxDuration = duration;
                }
            }

            return maxDuration;
        }

        /// <summary>
        /// 判断任一粒子系统是否启用了循环播放。
        /// </summary>
        internal static bool HasLoopEnabled(GameObject prefab)
        {
            ParticleSystem[] systems = GetParticleSystems(prefab);

            for (int i = 0; i < systems.Length; i++)
            {
                if (systems[i] != null && systems[i].main.loop)
                {
                    return true;
                }
            }

            return false;
        }
    }
}