using Godot;
using MemoryPack;

namespace Behide.Types;

public static class VariantConversion
{
    public static Variant ToVariant<T>(T value)
    {
        var bytes = MemoryPackSerializer.Serialize(value);
        return Variant.CreateFrom(bytes);
    }

    public static T? FromVariant<T>(Variant variant)
    {
        var bytes = variant.AsByteArray();
        return MemoryPackSerializer.Deserialize<T>(bytes);
    }
}

public partial record Player
{
    public static implicit operator Variant(Player player) =>
        VariantConversion.ToVariant(player);

    public static Player? FromVariant(Variant variant) =>
        VariantConversion.FromVariant<Player>(variant);
}

public abstract partial record PlayerState
{
    public static implicit operator Variant(PlayerState player) =>
        VariantConversion.ToVariant(player);

    public static PlayerState? FromVariant(Variant variant) =>
        VariantConversion.FromVariant<PlayerState>(variant);
}
