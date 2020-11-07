using System;

namespace MelonLoader
{
    public abstract class MelonPlugin : MelonBase
    {
        [Obsolete()]
        public MelonPluginInfoAttribute InfoAttribute { get => LegacyPluginInfo; }
        [Obsolete()]
        public MelonPluginGameAttribute[] GameAttributes { get => LegacyPluginGames; }

        public virtual void OnPreInitialization() { }
    }
}