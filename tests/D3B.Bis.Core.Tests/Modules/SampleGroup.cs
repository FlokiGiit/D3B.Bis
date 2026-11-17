using D3B.Bis.Core.Modules;

namespace D3B.Bis.Core.Tests.Modules;

internal sealed class SampleGroup : IModuleGroup
{
    public void Build(ModuleGroupBuilder group)
    {
        group.Add<RequiredModule>().Add<CountingModule>();
    }
}
