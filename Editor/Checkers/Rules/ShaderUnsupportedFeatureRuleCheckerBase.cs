using System.Collections.Generic;

namespace majsoul.tools.assetscoretool.editor
{
    public abstract class ShaderUnsupportedFeatureRuleCheckerBase
        : ShaderRuleCheckerBase
    {
        protected abstract EShaderUnsupportedFeature UnsupportedFeature { get; }

        public override AssetIssueResult Check(
            AssetContext context,
            ScoreRuleConfig ruleConfig)
        {
            List<ShaderPragma> pragmas;
            return GetIssueIfViolated(
                ruleConfig,
                TryGetPragmas(context, out pragmas) &&
                IsFeatureUsed(pragmas));
        }

        private bool IsFeatureUsed(List<ShaderPragma> pragmas)
        {
            switch (UnsupportedFeature)
            {
                case EShaderUnsupportedFeature.Geometry:
                    return ShaderRuleUtility.HasPragma(pragmas, "geometry");
                case EShaderUnsupportedFeature.Hull:
                    return ShaderRuleUtility.HasPragma(pragmas, "hull");
                case EShaderUnsupportedFeature.Domain:
                    return ShaderRuleUtility.HasPragma(pragmas, "domain");
                case EShaderUnsupportedFeature.Raytracing:
                    return ShaderRuleUtility.HasPragma(pragmas, "raytracing");
                case EShaderUnsupportedFeature.Target5:
                    return ShaderRuleUtility.HasTargetAtLeast(pragmas, 5.0m);
                case EShaderUnsupportedFeature.Compute:
                    return ShaderRuleUtility.HasPragma(pragmas, "compute");
                default:
                    return false;
            }
        }
    }

    public enum EShaderUnsupportedFeature
    {
        Geometry,
        Hull,
        Domain,
        Raytracing,
        Target5,
        Compute
    }

    public class ScoreRule_ShaderWebGeometry
        : ShaderUnsupportedFeatureRuleCheckerBase
    {
        protected override string RuleIdValue { get { return "basic.shader.web.geometry"; } }
        protected override EShaderUnsupportedFeature UnsupportedFeature { get { return EShaderUnsupportedFeature.Geometry; } }
    }

    public class ScoreRule_ShaderWebHull : ShaderUnsupportedFeatureRuleCheckerBase
    {
        protected override string RuleIdValue { get { return "basic.shader.web.hull"; } }
        protected override EShaderUnsupportedFeature UnsupportedFeature { get { return EShaderUnsupportedFeature.Hull; } }
    }

    public class ScoreRule_ShaderWebDomain : ShaderUnsupportedFeatureRuleCheckerBase
    {
        protected override string RuleIdValue { get { return "basic.shader.web.domain"; } }
        protected override EShaderUnsupportedFeature UnsupportedFeature { get { return EShaderUnsupportedFeature.Domain; } }
    }

    public class ScoreRule_ShaderWebRaytracing
        : ShaderUnsupportedFeatureRuleCheckerBase
    {
        protected override string RuleIdValue { get { return "basic.shader.web.raytracing"; } }
        protected override EShaderUnsupportedFeature UnsupportedFeature { get { return EShaderUnsupportedFeature.Raytracing; } }
    }

    public class ScoreRule_ShaderWebTarget5 : ShaderUnsupportedFeatureRuleCheckerBase
    {
        protected override string RuleIdValue { get { return "basic.shader.web.target5"; } }
        protected override EShaderUnsupportedFeature UnsupportedFeature { get { return EShaderUnsupportedFeature.Target5; } }
    }

    public class ScoreRule_ShaderWebCompute : ShaderUnsupportedFeatureRuleCheckerBase
    {
        protected override string RuleIdValue { get { return "basic.shader.web.compute"; } }
        protected override EShaderUnsupportedFeature UnsupportedFeature { get { return EShaderUnsupportedFeature.Compute; } }
    }

    public class ScoreRule_ShaderAndroidGeometry
        : ShaderUnsupportedFeatureRuleCheckerBase
    {
        protected override string RuleIdValue { get { return "basic.shader.android.geometry"; } }
        protected override EShaderUnsupportedFeature UnsupportedFeature { get { return EShaderUnsupportedFeature.Geometry; } }
    }

    public class ScoreRule_ShaderAndroidHull
        : ShaderUnsupportedFeatureRuleCheckerBase
    {
        protected override string RuleIdValue { get { return "basic.shader.android.hull"; } }
        protected override EShaderUnsupportedFeature UnsupportedFeature { get { return EShaderUnsupportedFeature.Hull; } }
    }

    public class ScoreRule_ShaderAndroidDomain
        : ShaderUnsupportedFeatureRuleCheckerBase
    {
        protected override string RuleIdValue { get { return "basic.shader.android.domain"; } }
        protected override EShaderUnsupportedFeature UnsupportedFeature { get { return EShaderUnsupportedFeature.Domain; } }
    }

    public class ScoreRule_ShaderAndroidRaytracing
        : ShaderUnsupportedFeatureRuleCheckerBase
    {
        protected override string RuleIdValue { get { return "basic.shader.android.raytracing"; } }
        protected override EShaderUnsupportedFeature UnsupportedFeature { get { return EShaderUnsupportedFeature.Raytracing; } }
    }

    public class ScoreRule_ShaderAndroidTarget5
        : ShaderUnsupportedFeatureRuleCheckerBase
    {
        protected override string RuleIdValue { get { return "basic.shader.android.target5"; } }
        protected override EShaderUnsupportedFeature UnsupportedFeature { get { return EShaderUnsupportedFeature.Target5; } }
    }

    public class ScoreRule_ShaderIOSGeometry
        : ShaderUnsupportedFeatureRuleCheckerBase
    {
        protected override string RuleIdValue { get { return "basic.shader.ios.geometry"; } }
        protected override EShaderUnsupportedFeature UnsupportedFeature { get { return EShaderUnsupportedFeature.Geometry; } }
    }

    public class ScoreRule_ShaderIOSHull : ShaderUnsupportedFeatureRuleCheckerBase
    {
        protected override string RuleIdValue { get { return "basic.shader.ios.hull"; } }
        protected override EShaderUnsupportedFeature UnsupportedFeature { get { return EShaderUnsupportedFeature.Hull; } }
    }

    public class ScoreRule_ShaderIOSDomain : ShaderUnsupportedFeatureRuleCheckerBase
    {
        protected override string RuleIdValue { get { return "basic.shader.ios.domain"; } }
        protected override EShaderUnsupportedFeature UnsupportedFeature { get { return EShaderUnsupportedFeature.Domain; } }
    }

    public class ScoreRule_ShaderIOSRaytracing
        : ShaderUnsupportedFeatureRuleCheckerBase
    {
        protected override string RuleIdValue { get { return "basic.shader.ios.raytracing"; } }
        protected override EShaderUnsupportedFeature UnsupportedFeature { get { return EShaderUnsupportedFeature.Raytracing; } }
    }

    public class ScoreRule_ShaderIOSTarget5 : ShaderUnsupportedFeatureRuleCheckerBase
    {
        protected override string RuleIdValue { get { return "basic.shader.ios.target5"; } }
        protected override EShaderUnsupportedFeature UnsupportedFeature { get { return EShaderUnsupportedFeature.Target5; } }
    }
}