using Aquamarine.Source.Input;
using Aquamarine.Source.Logging;
using Godot;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using System;

namespace Aquamarine.Source.Management.HUD;

public partial class MainMenu : Control
{
    [Export] public Button AccountButton;

    [Export] public Button AvatarsButton;
    [Export] public Button WorldsButton;
    [Export] public Button FriendsButton;
    [Export] public Button SettingsButton;

    [Export] public Button CloseButton;

    [Export] public Control MainTabContainer;
    [Export] public Control WorldsTab;
    [Export] public Control WorldsBrowserEntries;

    [Export] public Control ContextTabContainer;
    [Export] public Control WorldsContextTab;

    static readonly PackedScene world_browser_entry = ResourceLoader.Load<PackedScene>("res://Scenes/UI/Entries/world_browser_entry.tscn");

    public async override void _Ready()
    {
        base._Ready();
        
        WorldsButton.Pressed += () => SwitchMainTab(TabType.Worlds);

        CloseButton.Pressed += ToggleMenu;

        Visible = false;

        await FetchSessions();
        RefreshSessions();
    }

    DateTime lastFetch;
    List<SessionInfo> latestSessionsFetch;
    readonly JsonSerializerOptions jsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };
    private async Task FetchSessions()
    {
        if (DateTime.Now - lastFetch < TimeSpan.FromSeconds(5))
        {
            Logger.Warn("Server info fetch rate limit reached.");
        }
        lastFetch = DateTime.Now;
        try
        {
            GD.Print("Trying to get session list");

            using var client = new System.Net.Http.HttpClient();
            var response = await client.GetStringAsync(SessionInfo.SessionList);

            GD.Print("Got the session list");
            GD.Print(response);

            var sessions = JsonSerializer.Deserialize<List<SessionInfo>>(response, jsonSerializerOptions);

            if (sessions != null && sessions.Count != 0)
            {
                latestSessionsFetch = sessions;
            }
            else
            {
                Logger.Error("No valid sessions available in the API response.");
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Error fetching server info: {ex.Message}");
        }
    }
    void RefreshSessions()
    {
        Logger.Log("Refreshing sessions.");
        foreach (var entry in WorldsBrowserEntries.GetChildren())
        {
            entry.QueueFree();
        }
        foreach (var session in latestSessionsFetch)
        {
            AddWorld(session);
        }
    }
    void AddWorld(SessionInfo sessionInfo)
    {
        var entry = world_browser_entry.Instantiate<BrowserEntry>();
        entry.Setup(sessionInfo.Name, "👤 1.2k ");
        entry.QuickButton.Pressed += () =>
        {
            Logger.Log($"Joining session {sessionInfo.Name}.");
            ClientManager.Instance.JoinNatServer(sessionInfo.SessionIdentifier);
        };
        WorldsBrowserEntries.AddChild(entry);
    }

    async void SwitchMainTab(TabType tab)
    {
        switch (tab)
        {
            case TabType.Worlds:
                MainTabContainer.Visible = true;
                WorldsTab.Visible = true;
                await FetchSessions();
                RefreshSessions();
                break;
        }
    }
    void SwitchContextTab(TabType tab)
    {
        switch (tab)
        {
            case TabType.None:
                ContextTabContainer.Visible = false;
                WorldsContextTab.Visible = false;
                break;
            case TabType.Worlds:
                ContextTabContainer.Visible = true;
                WorldsContextTab.Visible = true;
                break;
        }
    }

    void ToggleMenu()
    {
        InputManager.MovementLocked = !InputManager.MovementLocked;
        Visible = InputManager.MovementLocked;
    }
}

public enum TabType
{
    None,
    Worlds
}
