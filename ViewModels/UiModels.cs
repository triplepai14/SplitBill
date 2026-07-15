using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using SplitBillApp.Services;

namespace SplitBillApp.ViewModels;

// A coloured circular avatar with a person's initial.
public class AvatarVM
{
    public string Initial { get; set; } = "?";
    public Color Background { get; set; } = Colors.Gray;
    public double Size { get; set; } = 32;
    public double FontSize { get; set; } = 13;
    public Thickness Margin { get; set; } = new(0);
    public Color BorderColor { get; set; } = Colors.Transparent;
    public double BorderWidth { get; set; }

    public static AvatarVM For(string name, int colorIndex, double size = 32,
        double marginLeft = 0, double borderWidth = 0)
        => new()
        {
            Initial = BillMath.Initial(name),
            Background = Color.FromArgb(BillMath.ColorForIndex(colorIndex)),
            Size = size,
            FontSize = Math.Round(size * 0.42),
            Margin = new Thickness(marginLeft, 0, 0, 0),
            BorderColor = borderWidth > 0 ? Colors.White : Colors.Transparent,
            BorderWidth = borderWidth,
        };
}

public enum ChipVariant { Category, Person, Payer, Exclude }

/// <summary>
/// A selectable pill. Its look derives entirely from <see cref="Active"/> and
/// <see cref="Variant"/> so the XAML only has to bind the computed colours.
/// For the Exclude variant, Active means "included in this item".
/// </summary>
public partial class ToggleChip : ObservableObject
{
    // Palette (mirrors the "Garden" theme tokens)
    static readonly Color Surface = Color.FromArgb("#FFFFFF");
    static readonly Color Sub = Color.FromArgb("#6A756F");
    static readonly Color Line = Color.FromArgb("#E7EAE4");
    static readonly Color Accent = Color.FromArgb("#1F8A5B");
    static readonly Color AccentSoft = Color.FromArgb("#E4F1EA");
    static readonly Color Text = Color.FromArgb("#16201A");
    static readonly Color MutedAvatar = Color.FromArgb("#B9BCB4");

    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Initial { get; set; } = "";
    public bool HasAvatar { get; set; }
    public Color AvatarColor { get; set; } = Colors.Gray;
    public ChipVariant Variant { get; set; } = ChipVariant.Person;

    // Marks the person who paid the bill — shown with a crown in the chip.
    [ObservableProperty] private bool isPayer;

    [ObservableProperty] private bool active;

    public ICommand? ToggleCommand { get; set; }

    public Color Background => Variant switch
    {
        ChipVariant.Category => Active ? Accent : Surface,
        ChipVariant.Payer => Active ? Accent : Surface,
        ChipVariant.Person => Active ? AccentSoft : Surface,
        ChipVariant.Exclude => Active ? AccentSoft : Colors.Transparent,
        _ => Surface,
    };

    public Color TextColor => Variant switch
    {
        ChipVariant.Category => Active ? Colors.White : Sub,
        ChipVariant.Payer => Active ? Colors.White : Sub,
        ChipVariant.Person => Active ? Text : Sub,
        ChipVariant.Exclude => Active ? Text : Sub,
        _ => Text,
    };

    public Color StrokeColor => Variant switch
    {
        ChipVariant.Category => Active ? Accent : Line,
        ChipVariant.Payer => Active ? Accent : Line,
        ChipVariant.Person => Active ? Accent : Line,
        ChipVariant.Exclude => Active ? Colors.Transparent : Line,
        _ => Line,
    };

    public Color AvatarBackground => Variant switch
    {
        ChipVariant.Payer => AvatarColor,
        ChipVariant.Person => Active ? AvatarColor : MutedAvatar,
        ChipVariant.Exclude => Active ? AvatarColor : MutedAvatar,
        _ => AvatarColor,
    };

    public TextDecorations TextDecorations =>
        Variant == ChipVariant.Exclude && !Active
            ? Microsoft.Maui.TextDecorations.Strikethrough
            : Microsoft.Maui.TextDecorations.None;

    partial void OnActiveChanged(bool value)
    {
        OnPropertyChanged(nameof(Background));
        OnPropertyChanged(nameof(TextColor));
        OnPropertyChanged(nameof(StrokeColor));
        OnPropertyChanged(nameof(AvatarBackground));
        OnPropertyChanged(nameof(TextDecorations));
    }
}
