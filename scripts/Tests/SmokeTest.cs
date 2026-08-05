using Godot;
using System.Linq;

// Headless smoke test for the template. Run it with:
//
//     ./run-smoke-test.sh
//
// or directly:
//
//     godot --headless res://scenes/SmokeTest.tscn
//
// Exits 0 when every check passes, 1 otherwise, so it works as a CI gate.
//
// This deliberately covers what compiling the project cannot: collision
// layer/mask values, NodePath exports pointing at the wrong node, signals
// that never reach their listener, and input actions that do not actually
// drive anything. Add a check here whenever you add a system.
public partial class SmokeTest : Node
{
    private const string SavePath = "user://savegame.json";

    private int _pass;
    private int _fail;
    private bool _hadExistingSave;
    private string _existingSave;

    private void Check(bool condition, string label)
    {
        if (condition)
        {
            _pass++;
            GD.Print($"  ok    {label}");
        }
        else
        {
            _fail++;
            GD.Print($"  FAIL  {label}");
        }
    }

    public override async void _Ready()
    {
        BackUpExistingSave();

        CheckInputMap();
        CheckStats();
        CheckInventory();
        CheckSaveLoad();
        CheckScenesLoad();
        await CheckLiveLevel();

        RestoreExistingSave();

        GD.Print($"\n==== {_pass} passed, {_fail} failed ====");
        GetTree().Quit(_fail == 0 ? 0 : 1);
    }

    // The test writes a real save file. Preserve whatever was already there so
    // running the suite never destroys a player's progress.
    private void BackUpExistingSave()
    {
        _hadExistingSave = FileAccess.FileExists(SavePath);
        if (!_hadExistingSave)
        {
            return;
        }

        using var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Read);
        _existingSave = file.GetAsText();
    }

    private void RestoreExistingSave()
    {
        if (_hadExistingSave)
        {
            using var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write);
            file.StoreString(_existingSave);
            return;
        }

        DirAccess.Open("user://")?.Remove("savegame.json");
    }

    private void CheckInputMap()
    {
        GD.Print("\ninput map");

        foreach (var action in new[] { "interact", "move_left", "move_right", "move_up", "move_down" })
        {
            Check(InputMap.HasAction(action), $"action exists: {action}");
        }

        var interact = InputMap.ActionGetEvents("interact");
        Check(interact.Any(e => e is InputEventKey k && (int)k.PhysicalKeycode == 69),
            "interact is bound to physical E");
        Check(interact.Any(e => e is InputEventJoypadButton b && (int)b.ButtonIndex == 0),
            "interact is bound to joypad button 0 (A)");

        // Every gameplay action needs a gamepad route, or the Steam Deck in
        // Game Mode cannot reach it. See AGENTS.md.
        foreach (var action in new[] { "interact", "move_left", "move_right", "move_up", "move_down" })
        {
            var events = InputMap.ActionGetEvents(action);
            Check(events.Any(e => e is InputEventJoypadButton or InputEventJoypadMotion),
                $"{action} has a gamepad binding");
            Check(events.All(e => e.Device == -1), $"{action} events all use device -1");
        }

        foreach (var action in new[] { "move_left", "move_right", "move_up", "move_down" })
        {
            var events = InputMap.ActionGetEvents(action);
            Check(events.Count(e => e is InputEventKey) == 2, $"{action} has arrow + WASD keys");
            Check(events.Any(e => e is InputEventJoypadMotion), $"{action} has a stick axis");
            Check(events.OfType<InputEventKey>().All(k => k.PhysicalKeycode != Key.None),
                $"{action} keys use physical keycodes");
        }
    }

    private void CheckStats()
    {
        GD.Print("\nstats");

        var stats = new PlayerStats();
        stats.TakeDamage(20);
        Check(stats.CurrentHealth == 85, "TakeDamage(20) against Defense 5 leaves 85 HP");
        stats.TakeDamage(1);
        Check(stats.CurrentHealth == 84, "damage below Defense still costs at least 1 HP");
        stats.Heal(999);
        Check(stats.CurrentHealth == 100, "Heal clamps to MaxHealth");
        stats.TakeDamage(9999);
        Check(stats.CurrentHealth == 0 && !stats.IsAlive, "health floors at 0 and IsAlive goes false");

        var leveling = new PlayerStats();
        leveling.AddExperience(100);
        Check(leveling.Level == 2 && leveling.MaxHealth == 120 && leveling.CurrentHealth == 120,
            "AddExperience(100) levels up and restores health");
        Check(leveling.ExperienceToNextLevel == 125, "XP requirement scales to 125");

        var multi = new PlayerStats();
        multi.AddExperience(1000);
        Check(multi.Level > 2, $"a large XP grant cascades multiple levels (reached {multi.Level})");
    }

    private void CheckInventory()
    {
        GD.Print("\ninventory");

        var inventory = new Inventory();
        AddChild(inventory);

        var potion = GD.Load<InventoryItem>("res://resources/items/health_potion.tres");
        Check(potion != null && potion.Id == "health_potion", "health_potion.tres loads with its Id");

        inventory.AddItem(potion, 3);
        Check(inventory.Slots.Count == 1 && inventory.Slots[0].Quantity == 3, "AddItem stacks to 3");
        inventory.RemoveItem("health_potion", 2);
        Check(inventory.Slots[0].Quantity == 1, "RemoveItem(2) leaves 1");
        inventory.RemoveItem("health_potion", 1);
        Check(inventory.Slots.Count == 0, "emptying a stack drops the slot");
        Check(!inventory.RemoveItem("nope", 1), "removing an absent item fails cleanly");
        Check(!inventory.AddItem(null), "AddItem(null) is rejected");

        inventory.QueueFree();
    }

    private void CheckSaveLoad()
    {
        GD.Print("\nsave/load");

        var stats = new PlayerStats { CharacterName = "Tester" };
        stats.AddExperience(100);
        SaveSystem.Save(stats);

        var loaded = SaveSystem.Load();
        Check(loaded != null, "Load returns a result after Save");
        Check(loaded?.CharacterName == "Tester", "character name round-trips");
        Check(loaded?.Level == stats.Level && loaded?.MaxHealth == stats.MaxHealth
              && loaded?.ExperienceToNextLevel == stats.ExperienceToNextLevel,
            "level, health and XP curve round-trip");
    }

    private void CheckScenesLoad()
    {
        GD.Print("\nscenes");

        foreach (var path in new[]
        {
            "res://scenes/TitleScreen.tscn",
            "res://scenes/Player.tscn",
            "res://scenes/NPC.tscn",
            "res://scenes/ItemPickup.tscn",
            "res://scenes/UI/HUD.tscn",
            "res://scenes/UI/DialogueBox.tscn",
            "res://scenes/World/TestLevel.tscn",
        })
        {
            Check(GD.Load<PackedScene>(path) != null, $"loads: {path}");
        }

        Check(GD.Load<PackedScene>(ProjectSettings.GetSetting("application/run/main_scene").AsString()) != null,
            "the configured main scene loads");
    }

    // Everything above is static. This section runs the actual game.
    private async System.Threading.Tasks.Task CheckLiveLevel()
    {
        GD.Print("\nlive level");

        var level = GD.Load<PackedScene>("res://scenes/World/TestLevel.tscn").Instantiate();
        AddChild(level);
        await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

        var player = level.GetNodeOrNull<PlayerController>("Player");
        var npc = level.GetNodeOrNull<NPC>("NPC");
        var pickup = level.GetNodeOrNull<ItemPickup>("HealthPotionPickup");

        Check(player != null, "Player is present");
        Check(npc != null, "NPC is present");
        Check(pickup != null, "ItemPickup is present");
        Check(level.GetNodeOrNull("HUD") != null && level.GetNodeOrNull("DialogueBox") != null,
            "HUD and DialogueBox are present");

        if (player == null || npc == null || pickup == null)
        {
            GD.Print("  (skipping live checks: the level is missing nodes)");
            return;
        }

        var inventory = player.GetNodeOrNull<Inventory>("Inventory");
        Check(inventory != null, "Player has an Inventory child");

        // Movement: proves the move_* actions actually reach the controller.
        var startX = player.GlobalPosition.X;
        Input.ActionPress("move_right");
        await WaitPhysics(10);
        Input.ActionRelease("move_right");
        Check(player.GlobalPosition.X > startX + 1f,
            $"move_right drives the player (x {startX:0.#} -> {player.GlobalPosition.X:0.#})");

        // Walls: proves the level's collision bodies actually block movement.
        player.GlobalPosition = new Vector2(60, 300);
        Input.ActionPress("move_left");
        await WaitPhysics(30);
        Input.ActionRelease("move_left");
        Check(player.GlobalPosition.X > 10f,
            $"the left wall stops the player (x {player.GlobalPosition.X:0.#})");

        // Pickup: proves the collision layer/mask pairing between the player
        // body and the pickup area.
        player.GlobalPosition = pickup.GlobalPosition;
        await WaitPhysics(10);
        Check(inventory != null && inventory.Slots.Count == 1, "walking onto ItemPickup grants the item");
        Check(!IsInstanceValid(pickup) || pickup.IsQueuedForDeletion(), "the pickup frees itself");

        // Dialogue: proves NPC -> EventBus -> DialogueBox is wired end to end.
        var dialogueFired = false;
        void OnDialogue(string _, string __) => dialogueFired = true;
        EventBus.Instance.DialogueStarted += OnDialogue;
        npc.Interact(player);
        Check(dialogueFired, "NPC.Interact emits DialogueStarted");
        Check(GetTree().Paused, "DialogueBox pauses the tree while open");
        EventBus.Instance.DialogueStarted -= OnDialogue;
        GetTree().Paused = false;

        // Health signal: proves stats changes reach listeners such as the HUD.
        var reported = -1;
        void OnHealth(int current, int _) => reported = current;
        EventBus.Instance.PlayerHealthChanged += OnHealth;
        player.Stats.TakeDamage(30);
        Check(reported == player.Stats.CurrentHealth, "TakeDamage propagates over EventBus");
        EventBus.Instance.PlayerHealthChanged -= OnHealth;

        level.QueueFree();
    }

    private async System.Threading.Tasks.Task WaitPhysics(int frames)
    {
        for (var i = 0; i < frames; i++)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
        }
    }
}
