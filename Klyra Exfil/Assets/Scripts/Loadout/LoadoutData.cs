using System;

namespace Klyra.Loadout
{
    [Serializable]
    public class LoadoutSlot
    {
        public string itemName;
        public int amount;
    }

    [Serializable]
    public class LoadoutData
    {
        public LoadoutSlot primary = new LoadoutSlot();
        public LoadoutSlot secondary = new LoadoutSlot();
        public LoadoutSlot throwable1 = new LoadoutSlot();
        public LoadoutSlot throwable2 = new LoadoutSlot();
        public LoadoutSlot primaryAmmo = new LoadoutSlot();
        public LoadoutSlot secondaryAmmo = new LoadoutSlot();
    }
}
