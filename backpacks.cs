using System.Collections.Generic;
using System.Linq;
using Oxide.Core;
using Rust;

namespace Oxide.Plugins
{
    [Info("Backpacks", "Melonoma", "1.2.0")]
    [Description("Allows players to craft and use backpacks that provide additional inventory slots")]
    public class Backpacks : RustPlugin
    {
        private const int LeatherRequired = 100;
        private const int ClothRequired = 100;
        private const int BackpackSlots = 15;
        private const string BackpackItemName = "backpack";
        private const string LeatherItemName = "leather";
        private const string ClothItemName = "cloth";
        private const string CraftingPermission = "backpacks.craft";
        private const string BackpackOpenKey = "e"; // Change this value to the desired key

        private int MaxBackpacksPerPlayer;

        private void Init()
        {
            permission.RegisterPermission(CraftingPermission, this);
        }

        private void LoadConfig()
        {
            MaxBackpacksPerPlayer = Config.Get<int>("MaxBackpacksPerPlayer", 1); // Default to 1 backpack per player
            SaveConfig();
            MaxBackpacksPerPlayer = Config.Get<int>("MaxBackpacksPerPlayer");
        }

        private void OnServerInitialized()
        {
            // Register the backpack item
            ItemDefinition backpackItemDef = ItemManager.FindItemDefinition(BackpackItemName);
            if (backpackItemDef == null)
            {
                backpackItemDef = new ItemDefinition();
                backpackItemDef.itemFlags = ItemDefinition.Flag.IsBlueprint | ItemDefinition.Flag.Hidden;
                backpackItemDef.displayName = "Backpack";
                backpackItemDef.shortname = BackpackItemName;
                backpackItemDef.stackable = 1;
                backpackItemDef.category = ItemCategory.Medical;
                backpackItemDef.condition.enabled = true;
                backpackItemDef.condition.max = 500f;
                backpackItemDef.Blueprint = new ItemBlueprint();
                backpackItemDef.Blueprint.ingredients = new[]
                {
                    new ItemAmount
                    {
                        itemDef = ItemManager.FindItemDefinition(LeatherItemName),
                        amount = LeatherRequired
                    },
                    new ItemAmount
                    {
                        itemDef = ItemManager.FindItemDefinition(ClothItemName),
                        amount = ClothRequired
                    }
                };
                backpackItemDef.Blueprint.defaultBlueprint = true;
                ItemManager.itemList.Add(backpackItemDef);
            }
        }
      

        private void OnItemCraft(ItemCraftTask task)
        {
            try
            {

            if (task.blueprint == null || task.blueprint.targetItem.shortname != BackpackItemName) return;

            BasePlayer player = task.owner;
            if (player == null || !permission.UserHasPermission(player.UserIDString, CraftingPermission))
        {
                task.Cancel();
                player.ChatMessage("You don't have permission to craft backpacks");
                return;
        }

            int leatherAmount = task.blueprint.ingredients[0].amount;
            if (player.inventory.GetAmount(ItemManager.FindItemDefinition(LeatherItemName).itemid) < leatherAmount)
        {
                task.Cancel();
                player.ChatMessage($"You need {leatherAmount} {LeatherItemName} to craft a backpack");
                return;
        }
            int clothAmount = task.blueprint.ingredients[1].amount; // corrected line
            if (player.inventory.GetAmount(ItemManager.FindItemDefinition(ClothItemName).itemid) < clothAmount)
        {
                task.Cancel();
                player.ChatMessage($"You need {clothAmount} {ClothItemName} to craft a backpack");
                return;
        }
            }
            catch (Exception ex)
        {
            // Handle the exception here
            Debug.Log("An error occurred while crafting backpacks: " + ex.Message);
    }

    player.inventory.Take(null, ItemManager.FindItemDefinition(LeatherItemName).itemid, leatherAmount);
    player.inventory.Take(null, ItemManager.FindItemDefinition(ClothItemName).itemid, clothAmount);
}
        private void OnItemDeployed(Deployer deployer, BaseEntity entity)
    {
            if (entity == null || deployer == null || deployer.GetOwnerPlayer() == null) return;

            // Check if the player already has the maximum number of backpacks allowed
            BasePlayer player = deployer.GetOwnerPlayer();
            int numBackpacks = player.inventory.containerWear.itemList.Count(item => item.info.shortname == BackpackItemName);
            if (numBackpacks >= MaxBackpacksPerPlayer)
        {
            player.ChatMessage($"You can only carry {MaxBackpacksPerPlayer} backpacks at a time");
            return;
        }    

            Item backpackItem = deployer.GetOwnerPlayer().inventory.containerBelt.GetSlot(0);
            if (backpackItem == null || backpackItem.info.shortname != BackpackItemName) return;

            // Make the backpack invisible and enable its UI
            entity.SetFlag(BaseEntity.Flags.Reserved5, true);
            entity.SendNetworkUpdateImmediate();

            ItemContainer container = entity.GetComponent<ItemContainer>();
            if (container != null)
        {
            container.isServer = true;
            container.allowedContents = ItemContainer.ContentsType.Generic;
            container.ServerInitialize(null, BackpackSlots);
        }

            // Set the backpack to use the default bodybag model
            entity.skinID = 1540583365; // Bodybag skin ID

            // Open the backpack inventory when the player presses the backpack open key
            BaseEntity backpackEntity = backpackItem.GetHeldEntity();
            if (backpackEntity != null)
        {
            backpackEntity.GetComponent<DeployedBackpack>().backpackEntity.GetComponent<DeployedBackpack>()?.SetDeployer(deployer);
        }
}
