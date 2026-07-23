using Godot;

namespace Behide.Prefabs.Player;

public partial class WeaponManager : Node
{
    private Dictionary<string, Weapon> WeaponsDict;
    public Weapon ActiveWeapon;
    private Weapon Secondary;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        WeaponsDict = GetChildren().Cast<Weapon>().ToDictionary<Weapon, string>(el =>
        {
            GD.PrintErr(el.Name);
            return el.Name;
        });
        WeaponsDict.TryGetValue("SubmachineGun", out ActiveWeapon);
        WeaponsDict.TryGetValue("Sword", out Secondary);
        ActiveWeapon.SetVisible(true);
    }

    public void Change()
    {
        (ActiveWeapon, Secondary) = (Secondary, ActiveWeapon);
        ActiveWeapon.SetVisible(true);
        Secondary.SetVisible(false);
    }
}
