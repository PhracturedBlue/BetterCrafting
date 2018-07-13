﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static BetterCrafting.CategoryManager;

namespace BetterCrafting
{
    class BetterCraftingPage : IClickableMenu
    {
        private const int WIDTH = 800;
        private const string AVAILABLE = "a";
        private const string UNAVAILABLE = "u";
        private const string UNKNOWN = "k";

        private int pageX;
        private int pageY;

        private ModEntry betterCrafting;

        private CategoryManager categoryManager;

        private InventoryMenu inventory;

        private Dictionary<ItemCategory, Dictionary<ClickableTextureComponent, CraftingRecipe>> recipes;
        private Dictionary<ClickableComponent, ItemCategory> categories;

        private ItemCategory selectedCategory;

        private ClickableTextureComponent trashCan;
        private float trashCanLidRotation;

        private ClickableTextureComponent oldButton;

        private CraftingRecipe hoverRecipe;

        private string hoverTitle;
        private string hoverText;
        private Item heldItem;
        private Item hoverItem;

        private string categoryText;

        public BetterCraftingPage(ModEntry betterCrafting, CategoryData categoryData)
            : base(Game1.activeClickableMenu.xPositionOnScreen, Game1.activeClickableMenu.yPositionOnScreen, Game1.activeClickableMenu.width, Game1.activeClickableMenu.height)
        {
            this.betterCrafting = betterCrafting;

            this.categoryManager = new CategoryManager(betterCrafting.Monitor, categoryData);

            this.inventory = new InventoryMenu(this.xPositionOnScreen + IClickableMenu.spaceToClearSideBorder + IClickableMenu.borderWidth,
                this.yPositionOnScreen + IClickableMenu.spaceToClearTopBorder + IClickableMenu.borderWidth + Game1.tileSize * 5 - Game1.tileSize / 4,
                false);
            this.inventory.showGrayedOutSlots = true;

            this.pageX = this.xPositionOnScreen + IClickableMenu.spaceToClearSideBorder + IClickableMenu.borderWidth - Game1.tileSize / 4;
            this.pageY = this.yPositionOnScreen + IClickableMenu.spaceToClearTopBorder + IClickableMenu.borderWidth - Game1.tileSize / 4;

            this.selectedCategory = this.categoryManager.GetDefaultItemCategory();

            this.recipes = new Dictionary<ItemCategory, Dictionary<ClickableTextureComponent, CraftingRecipe>>();
            this.categories = new Dictionary<ClickableComponent, ItemCategory>();

            int catIndex = 0;

            var categorySpacing = Game1.tileSize / 6;
            var tabPad = Game1.tileSize + Game1.tileSize / 4;

            foreach (ItemCategory category in this.categoryManager.GetItemCategories())
            {
                this.recipes[category] = new Dictionary<ClickableTextureComponent, CraftingRecipe>();

                var catName = this.categoryManager.GetItemCategoryName(category);

                var nameSize = Game1.smallFont.MeasureString(catName);

                var width = nameSize.X + Game1.tileSize / 2;
                var height = nameSize.Y + Game1.tileSize / 4;

                var x = this.xPositionOnScreen - width;
                var y = this.yPositionOnScreen + tabPad + catIndex * (height + categorySpacing);

                var c = new ClickableComponent(
                    new Rectangle((int) x, (int) y, (int) width, (int) height),
                    category.Equals(this.selectedCategory) ? UNAVAILABLE : AVAILABLE, catName);

                this.categories.Add(c, category);

                catIndex += 1;
            }

            this.trashCan = new ClickableTextureComponent(
                new Rectangle(
                    this.xPositionOnScreen + width + 4,
                    this.yPositionOnScreen + height - Game1.tileSize * 3 - Game1.tileSize / 2 - IClickableMenu.borderWidth - 104,
                    Game1.tileSize, 104),
                Game1.mouseCursors,
                new Rectangle(669, 261, 16, 26),
                (float)Game1.pixelZoom, false);

            this.oldButton = new ClickableTextureComponent("",
                new Rectangle(
                    this.xPositionOnScreen + width,
                    this.yPositionOnScreen + height / 3 - Game1.tileSize + Game1.pixelZoom * 2,
                    Game1.tileSize,
                    Game1.tileSize),
                "",
                "Old Crafting Menu",
                Game1.mouseCursors,
                new Rectangle(162, 440, 16, 16),
                (float)Game1.pixelZoom, false);

            this.UpdateInventory();
        }

        public void UpdateInventory()
        {
            foreach (var category in this.recipes.Keys)
            {
                this.recipes[category].Clear();
            }

            var indexMap = new Dictionary<ItemCategory, int>();

            var spaceBetweenCraftingIcons = Game1.tileSize / 4;
            int maxItemsInRow = (this.width - IClickableMenu.spaceToClearSideBorder - IClickableMenu.borderWidth) / (Game1.tileSize + spaceBetweenCraftingIcons);
            var xPad = Game1.tileSize / 8;

            foreach (var recipeName in CraftingRecipe.craftingRecipes.Keys)
            {
                var recipe = new CraftingRecipe(recipeName, false);
                var item = recipe.createItem();

                var category = this.categoryManager.GetItemCategory(item);

                if (!indexMap.ContainsKey(category))
                {
                    indexMap.Add(category, 0);
                }

                var column = indexMap[category] % maxItemsInRow;
                var row = indexMap[category] / maxItemsInRow;

                var x = this.pageX + xPad + column * (Game1.tileSize + spaceBetweenCraftingIcons);
                var y = this.pageY + row * (Game1.tileSize * 2 + spaceBetweenCraftingIcons);

                var hoverText = Game1.player.craftingRecipes.ContainsKey(recipeName) ? (
                    recipe.doesFarmerHaveIngredientsInInventory() ? AVAILABLE : UNAVAILABLE)
                    : UNKNOWN;

                var c = new ClickableTextureComponent("",
                    new Rectangle(x, y, Game1.tileSize, recipe.bigCraftable ? (Game1.tileSize * 2) : Game1.tileSize),
                    null,
                    hoverText,
                    recipe.bigCraftable ? Game1.bigCraftableSpriteSheet : Game1.objectSpriteSheet,
                    recipe.bigCraftable ? Game1.getArbitrarySourceRect(
                        Game1.bigCraftableSpriteSheet,
                        16, 32,
                        recipe.getIndexOfMenuView())
                    : Game1.getSourceRectForStandardTileSheet(
                        Game1.objectSpriteSheet,
                        recipe.getIndexOfMenuView(), 16, 16),
                    (float)Game1.pixelZoom,
                    false);

                this.recipes[category].Add(c, recipe);

                indexMap[category] += 1;
            }
        }

        public override bool readyToClose()
        {
            return this.heldItem == null;
        }

        public override void performHoverAction(int x, int y)
        {
            base.performHoverAction(x, y);

            this.hoverRecipe = null;
            this.hoverItem = this.inventory.hover(x, y, this.hoverItem);

            if (this.hoverItem != null)
            {
                this.hoverTitle = this.inventory.hoverTitle;
                this.hoverText = this.inventory.hoverText;
            }
            else
            {
                this.hoverTitle = "";
                this.hoverText = "";
            }

            foreach (var c in this.recipes[this.selectedCategory].Keys)
            {
                if (c.containsPoint(x, y))
                {
                    if (c.hoverText.Equals(UNKNOWN))
                    {
                        this.hoverText = this.recipes[this.selectedCategory][c].name + " (unknown)";
                    }
                    else
                    {
                        this.hoverRecipe = this.recipes[this.selectedCategory][c];
                    }

                    if (c.hoverText.Equals(AVAILABLE))
                    {
                        c.scale = Math.Min(c.scale + 0.02f, c.baseScale + 0.2f);
                    }
                }
                else
                {
                    c.scale = Math.Max(c.scale - 0.02f, c.baseScale);
                }
            }

            this.categoryText = null;

            foreach (var c in this.categories.Keys)
            {
                if (c.containsPoint(x, y))
                {
                    this.categoryText = c.label;
                }
            }

            this.oldButton.tryHover(x, y);
            if (this.oldButton.containsPoint(x, y))
            {
                this.hoverText = oldButton.hoverText;
            }

            if (this.trashCan.containsPoint(x, y))
            {
                if (this.trashCanLidRotation <= 0f)
                {
                    Game1.playSound("trashcanlid");
                }
                this.trashCanLidRotation = Math.Min(this.trashCanLidRotation + 0.06544985f, 1.57079637f);
                return;
            }

            this.trashCanLidRotation = Math.Max(this.trashCanLidRotation - 0.06544985f, 0f);
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            base.receiveLeftClick(x, y, playSound);

            this.heldItem = this.inventory.leftClick(x, y, this.heldItem);

            foreach (var c in this.categories.Keys)
            {
                if (c.containsPoint(x, y))
                {
                    if (!this.selectedCategory.Equals(this.categories[c]))
                    {
                        Game1.playSound("smallSelect");
                    }

                    foreach (var c2 in this.categories.Keys)
                    {
                        c2.name = AVAILABLE;
                    }

                    c.name = UNAVAILABLE;

                    this.selectedCategory = this.categories[c];
                }
            }

            foreach (var c in this.recipes[this.selectedCategory].Keys)
            {
                if (c.containsPoint(x, y)
                    && c.hoverText.Equals(AVAILABLE)
                    && this.recipes[this.selectedCategory][c].doesFarmerHaveIngredientsInInventory())
                {
                    this.clickCraftingRecipe(c, true);
                }
            }

            if (this.oldButton.containsPoint(x, y) && this.readyToClose())
            {
                Game1.playSound("select");

                GameMenu gameMenu = (GameMenu)Game1.activeClickableMenu;
                var pages = this.betterCrafting.Helper.Reflection.GetField<List<IClickableMenu>>(gameMenu, "pages").GetValue();
                pages[gameMenu.currentTab] = new CraftingPage(this.xPositionOnScreen, this.yPositionOnScreen, this.width, this.height, false);

                return;
            }

            if (this.trashCan.containsPoint(x, y) && this.heldItem != null && this.heldItem.canBeTrashed())
            {
                if (this.heldItem is StardewValley.Object && Game1.player.specialItems.Contains((this.heldItem as StardewValley.Object).ParentSheetIndex))
                {
                    Game1.player.specialItems.Remove((this.heldItem as StardewValley.Object).ParentSheetIndex);
                }
                this.heldItem = null;
                Game1.playSound("trashcan");

                return;
            }

            if (this.heldItem != null && !this.isWithinBounds(x, y) && this.heldItem.canBeDropped())
            {
                Game1.playSound("throwDownItem");
                Game1.createItemDebris(this.heldItem, Game1.player.getStandingPosition(), Game1.player.facingDirection);
                this.heldItem = null;

                return;
            }
        }

        private void clickCraftingRecipe(ClickableTextureComponent c, bool playSound)
        {
            CraftingRecipe recipe = this.recipes[this.selectedCategory][c];
            Item crafted = recipe.createItem();

            Game1.player.checkForQuestComplete(null, -1, -1, crafted, null, 2, -1);

            if (this.heldItem == null)
            {
                recipe.consumeIngredients();
                this.heldItem = crafted;

                if (playSound)
                {
                    Game1.playSound("coin");
                }
            }
            else if (this.heldItem.Name.Equals(crafted.Name) && this.heldItem.Stack + recipe.numberProducedPerCraft - 1 < this.heldItem.maximumStackSize())
            {
                recipe.consumeIngredients();
                this.heldItem.Stack += recipe.numberProducedPerCraft;

                if (playSound)
                {
                    Game1.playSound("coin");
                }
            }

            Game1.player.craftingRecipes[recipe.name] += recipe.numberProducedPerCraft;

            Game1.stats.checkForCraftingAchievements();

            if (Game1.options.gamepadControls && this.heldItem != null && Game1.player.couldInventoryAcceptThisItem(this.heldItem))
            {
                Game1.player.addItemToInventoryBool(this.heldItem);
                this.heldItem = null;
            }
        }

        public override void receiveRightClick(int x, int y, bool playSound = true)
        {
            base.receiveRightClick(x, y, playSound);

            this.heldItem = this.inventory.rightClick(x, y, this.heldItem);

            foreach (var c in this.recipes[this.selectedCategory].Keys)
            {
                if (c.containsPoint(x, y)
                    && c.hoverText.Equals(AVAILABLE)
                    && this.recipes[this.selectedCategory][c].doesFarmerHaveIngredientsInInventory())
                {
                    this.clickCraftingRecipe(c, true);
                }
            }
        }

        public override void draw(SpriteBatch b)
        {
            base.drawHorizontalPartition(b, this.yPositionOnScreen + IClickableMenu.borderWidth + IClickableMenu.spaceToClearTopBorder + Game1.tileSize * 4);
            
            foreach (var c in this.recipes[this.selectedCategory].Keys)
            {
                if (c.hoverText.Equals(AVAILABLE))
                {
                    c.draw(b, Color.White, 0.89f);
                }
                else if (c.hoverText.Equals(UNKNOWN))
                {
                    c.draw(b, new Color(0f, 0f, 0f, 0.1f), 0.89f);
                }
                else
                {
                    c.draw(b, Color.Gray * 0.4f, 0.89f);
                }
            }

            foreach (var c in this.categories.Keys)
            {
                var boxColor = Color.White;
                var textColor = Game1.textColor;

                if (c.name.Equals(UNAVAILABLE))
                {
                    boxColor = Color.Gray;
                }

                IClickableMenu.drawTextureBox(b,
                    Game1.menuTexture,
                    new Rectangle(0, 256, 60, 60),
                    c.bounds.X,
                    c.bounds.Y,
                    c.bounds.Width,
                    c.bounds.Height + Game1.tileSize / 16,
                    boxColor);

                b.DrawString(Game1.smallFont,
                    c.label,
                    new Vector2(c.bounds.X + Game1.tileSize / 4, c.bounds.Y + Game1.tileSize / 4),
                    textColor);
            }

            this.inventory.draw(b);

            this.oldButton.draw(b, this.readyToClose() ? Color.White : Color.Gray, 0.89f);

            this.trashCan.draw(b);
            b.Draw(
                Game1.mouseCursors,
                new Vector2((float)(this.trashCan.bounds.X + 60), (float)(this.trashCan.bounds.Y + 40)),
                new Rectangle(686, 256, 18, 10),
                Color.White,
                this.trashCanLidRotation,
                new Vector2(16f, 10f),
                Game1.pixelZoom,
                SpriteEffects.None,
                0.86f);

            base.drawMouse(b);

            if (this.hoverItem != null)
            {
                IClickableMenu.drawToolTip(
                    b,
                    this.hoverText,
                    this.hoverTitle,
                    this.hoverItem,
                    this.heldItem != null);
            }
            else if (this.hoverText != null)
            {
                IClickableMenu.drawHoverText(b,
                    this.hoverText,
                    Game1.smallFont,
                    (this.heldItem != null) ? Game1.tileSize : 0,
                    (this.heldItem != null) ? Game1.tileSize : 0);
            }

            if (this.heldItem != null)
            {
                this.heldItem.drawInMenu(b,
                    new Vector2(
                        (float)(Game1.getOldMouseX() + Game1.tileSize / 4),
                        (float)(Game1.getOldMouseY() + Game1.tileSize / 4)),
                    1f);
            }

            if (this.hoverRecipe != null)
            {
                IClickableMenu.drawHoverText(b,
                    " ",
                    Game1.smallFont,
                    Game1.tileSize * 3 / 4,
                    Game1.tileSize * 3 / 4,
                    -1,
                    this.hoverRecipe.name,
                    -1,
                    null,
                    null, 0, -1, -1, -1, -1, 1f, this.hoverRecipe);
            }
            else if (this.categoryText != null)
            {
                IClickableMenu.drawHoverText(b, this.categoryText, Game1.smallFont);
            }
        }
    }
}