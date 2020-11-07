using System;

namespace MelonLoader
{
    public abstract class MelonMod : MelonBase
    {
        [Obsolete]
        public MelonModInfoAttribute InfoAttribute => LegacyModInfo;

        [Obsolete]
        public MelonModGameAttribute[] GameAttributes => LegacyModGames;

        public virtual void OnLevelIsLoading() {}
        public virtual void OnLevelWasLoaded(int level) {}
        public virtual void OnLevelWasInitialized(int level) {}
        public virtual void OnFixedUpdate() {}
    }
}