using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using BibleFamilyTreeBuilder.App.Models;
using BibleFamilyTreeBuilder.App.Services;
using BibleFamilyTreeBuilder.App.Theming;
using Microsoft.Win32;

namespace BibleFamilyTreeBuilder.App.Controls;

public partial class TreeCanvas : UserControl
{
    public static readonly DependencyProperty ProjectProperty =
        DependencyProperty.Register(
            nameof(Project),
            typeof(TreeProject),
            typeof(TreeCanvas),
            new PropertyMetadata(null, OnProjectChanged));

    public static readonly DependencyProperty SelectedPersonProperty =
        DependencyProperty.Register(
            nameof(SelectedPerson),
            typeof(Person),
            typeof(TreeCanvas),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedPersonChanged));

    public static readonly DependencyProperty ZoomProperty =
        DependencyProperty.Register(
            nameof(Zoom),
            typeof(double),
            typeof(TreeCanvas),
            new PropertyMetadata(1.0, OnZoomChanged));

    public static readonly DependencyProperty ExportTitleProperty =
        DependencyProperty.Register(
            nameof(ExportTitle),
            typeof(string),
            typeof(TreeCanvas),
            new PropertyMetadata("Full Tree"));

    private readonly TreeLayoutService _layoutService = new();
    private const double ExportScale = 2.0;
    private const double FullTreeExportPadding = 72;
    private const double FullTreeExportHeaderHeight = 132;
    private const double ExportLegendWidth = 282;
    private const double ExportLegendHeight = 154;
    private Point _lastPanPoint;
    private bool _isPanning;

    public TreeCanvas()
    {
        InitializeComponent();
        Loaded += (_, _) => Refresh();
        ThemeManager.ThemeChanged += OnThemeChanged;
        Unloaded += (_, _) => ThemeManager.ThemeChanged -= OnThemeChanged;
    }

    private static ThemePalette Palette => ThemeManager.Current.Palette;

    private void OnThemeChanged()
    {
        Refresh();
    }

    public TreeProject? Project
    {
        get => (TreeProject?)GetValue(ProjectProperty);
        set => SetValue(ProjectProperty, value);
    }

    public Person? SelectedPerson
    {
        get => (Person?)GetValue(SelectedPersonProperty);
        set => SetValue(SelectedPersonProperty, value);
    }

    public double Zoom
    {
        get => (double)GetValue(ZoomProperty);
        set => SetValue(ZoomProperty, value);
    }

    public string ExportTitle
    {
        get => (string)GetValue(ExportTitleProperty);
        set => SetValue(ExportTitleProperty, value);
    }

    public void Refresh()
    {
        PART_Canvas.Children.Clear();

        if (Project is null)
        {
            return;
        }

        var layout = _layoutService.Layout(Project);
        PART_Canvas.Width = layout.Width;
        PART_Canvas.Height = layout.Height;

        DrawScene(PART_Canvas, Project, layout, includeCardInteraction: true);
    }

    public void ResetView()
    {
        PanTransform.X = 0;
        PanTransform.Y = 0;
        ScaleTransform.ScaleX = Zoom;
        ScaleTransform.ScaleY = Zoom;
    }

    public void CenterSelectedPerson()
    {
        if (Project is null || SelectedPerson is null)
        {
            return;
        }

        var layout = _layoutService.Layout(Project);
        if (!layout.PersonBounds.TryGetValue(SelectedPerson.Id, out var bounds))
        {
            return;
        }

        var centerX = bounds.Left + bounds.Width / 2;
        var centerY = bounds.Top + bounds.Height / 2;
        PanTransform.X = ActualWidth / 2 - centerX * Zoom;
        PanTransform.Y = ActualHeight / 2 - centerY * Zoom;
    }

    public void FitTreeToView()
    {
        if (Project is null || ActualWidth <= 0 || ActualHeight <= 0)
        {
            return;
        }

        var layout = _layoutService.Layout(Project);
        var availableWidth = Math.Max(220, ActualWidth - 72);
        var availableHeight = Math.Max(220, ActualHeight - 72);
        Zoom = Math.Clamp(Math.Min(availableWidth / layout.Width, availableHeight / layout.Height), 0.35, 1.45);

        PanTransform.X = Math.Max(24, (ActualWidth - layout.Width * Zoom) / 2);
        PanTransform.Y = Math.Max(24, (ActualHeight - layout.Height * Zoom) / 2);
    }

    public void ExportCurrentViewAsPng()
    {
        var dialog = CreateSaveDialog("bible-family-tree-current-view.png");
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            if (ActualWidth <= 0 || ActualHeight <= 0)
            {
                throw new InvalidOperationException("The tree canvas is not ready to export yet.");
            }

            var bitmap = new RenderTargetBitmap(
                Math.Max(1, (int)Math.Ceiling(ActualWidth * ExportScale)),
                Math.Max(1, (int)Math.Ceiling(ActualHeight * ExportScale)),
                96 * ExportScale,
                96 * ExportScale,
                PixelFormats.Pbgra32);

            bitmap.Render(this);
            SaveBitmap(bitmap, dialog.FileName);
        }
        catch (Exception ex)
        {
            ShowExportError(ex);
        }
    }

    public void ExportFullTreeAsPng()
    {
        var dialog = CreateSaveDialog("bible-family-tree-full-tree.png");
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            if (Project is null)
            {
                throw new InvalidOperationException("There is no tree to export.");
            }

            var layout = _layoutService.Layout(Project);
            var exportWidth = Math.Max(layout.Width + FullTreeExportPadding * 2, 760);
            var exportHeight = layout.Height + FullTreeExportPadding * 2 + FullTreeExportHeaderHeight;
            var exportCanvas = new Canvas
            {
                Width = exportWidth,
                Height = exportHeight,
                Background = new SolidColorBrush(Palette.CanvasBackground)
            };

            var background = new Rectangle
            {
                Width = exportWidth,
                Height = exportHeight,
                Fill = new SolidColorBrush(Palette.CanvasBackground)
            };
            exportCanvas.Children.Add(background);

            var header = CreateExportHeader(exportWidth);
            Canvas.SetLeft(header, FullTreeExportPadding);
            Canvas.SetTop(header, 28);
            exportCanvas.Children.Add(header);

            var legend = CreateLegendPanel(includeShadow: false);
            Canvas.SetLeft(legend, exportWidth - FullTreeExportPadding - ExportLegendWidth);
            Canvas.SetTop(legend, 22);
            exportCanvas.Children.Add(legend);

            var treeCanvas = new Canvas
            {
                Width = layout.Width,
                Height = layout.Height,
                Background = new SolidColorBrush(Palette.CanvasBackground)
            };

            DrawScene(treeCanvas, Project, layout, includeCardInteraction: false);
            Canvas.SetLeft(treeCanvas, FullTreeExportPadding);
            Canvas.SetTop(treeCanvas, FullTreeExportHeaderHeight + FullTreeExportPadding);
            exportCanvas.Children.Add(treeCanvas);

            exportCanvas.Measure(new Size(exportWidth, exportHeight));
            exportCanvas.Arrange(new Rect(0, 0, exportWidth, exportHeight));
            exportCanvas.UpdateLayout();

            var bitmap = new RenderTargetBitmap(
                Math.Max(1, (int)Math.Ceiling(exportWidth * ExportScale)),
                Math.Max(1, (int)Math.Ceiling(exportHeight * ExportScale)),
                96 * ExportScale,
                96 * ExportScale,
                PixelFormats.Pbgra32);

            bitmap.Render(exportCanvas);
            SaveBitmap(bitmap, dialog.FileName);
        }
        catch (Exception ex)
        {
            ShowExportError(ex);
        }
    }

    private static void OnProjectChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
    {
        ((TreeCanvas)dependencyObject).Refresh();
    }

    private static void OnSelectedPersonChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
    {
        ((TreeCanvas)dependencyObject).Refresh();
    }

    private static void OnZoomChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
    {
        var canvas = (TreeCanvas)dependencyObject;
        var zoom = (double)e.NewValue;
        canvas.ScaleTransform.ScaleX = zoom;
        canvas.ScaleTransform.ScaleY = zoom;
    }

    private void DrawScene(Canvas targetCanvas, TreeProject project, TreeLayoutResult layout, bool includeCardInteraction)
    {
        DrawGenerationBands(targetCanvas, layout);
        DrawRelationshipLines(targetCanvas, project, layout);
        DrawPersonCards(targetCanvas, project, layout, includeCardInteraction);
    }

    private void DrawGenerationBands(Canvas targetCanvas, TreeLayoutResult layout)
    {
        foreach (var lane in layout.GenerationLanes)
        {
            var fill = lane.Generation % 2 == 0
                ? new SolidColorBrush(Palette.BandEven)
                : new SolidColorBrush(Palette.BandOdd);

            var band = new Rectangle
            {
                Width = layout.Width,
                Height = lane.Height,
                Fill = fill,
                Stroke = new SolidColorBrush(Palette.BandBorder),
                StrokeThickness = 1
            };

            Canvas.SetLeft(band, 0);
            Canvas.SetTop(band, lane.Top);
            targetCanvas.Children.Add(band);

            var label = new Border
            {
                Width = 236,
                Height = 34,
                Background = new SolidColorBrush(Palette.GenLabelBackground),
                BorderBrush = new SolidColorBrush(Palette.GenLabelBorder),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8, 4, 8, 4),
                Child = new TextBlock
                {
                    Text = lane.DisplayName,
                    FontSize = 12,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Palette.GenLabelText),
                    TextWrapping = TextWrapping.Wrap,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };

            Canvas.SetLeft(label, 24);
            Canvas.SetTop(label, lane.Top + 14);
            targetCanvas.Children.Add(label);
        }
    }

    private void DrawRelationshipLines(Canvas targetCanvas, TreeProject project, TreeLayoutResult layout)
    {
        foreach (var relationship in project.Relationships)
        {
            if (!layout.PersonBounds.TryGetValue(relationship.FromPersonId, out var fromBounds) ||
                !layout.PersonBounds.TryGetValue(relationship.ToPersonId, out var toBounds))
            {
                continue;
            }

            if (relationship.Type == RelationshipType.Marriage)
            {
                var labelPoint = DrawMarriageLine(targetCanvas, fromBounds, toBounds);
                DrawRelationshipLabel(targetCanvas, relationship, labelPoint);
            }
            else
            {
                var labelPoint = DrawParentChildLine(targetCanvas, fromBounds, toBounds, relationship);
                DrawRelationshipLabel(targetCanvas, relationship, labelPoint);
            }
        }
    }

    private Point DrawParentChildLine(Canvas targetCanvas, Rect parentBounds, Rect childBounds, Relationship relationship)
    {
        var start = new Point(parentBounds.Left + parentBounds.Width / 2, parentBounds.Bottom);
        var end = new Point(childBounds.Left + childBounds.Width / 2, childBounds.Top);
        var midY = start.Y + Math.Max(24, (end.Y - start.Y) / 2);
        var stroke = relationship.ParentKind is ParentKind.Adopted or ParentKind.Legal
            ? new SolidColorBrush(Palette.AdoptedLegalLine)
            : new SolidColorBrush(Palette.ParentChildLine);

        var path = new System.Windows.Shapes.Path
        {
            Stroke = stroke,
            StrokeThickness = relationship.ParentKind is ParentKind.Adopted or ParentKind.Legal ? 2.6 : 2.0,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
            StrokeLineJoin = PenLineJoin.Round,
            Data = new PathGeometry([
                new PathFigure(start, [
                    new LineSegment(new Point(start.X, midY), true),
                    new LineSegment(new Point(end.X, midY), true),
                    new LineSegment(end, true)
                ], false)
            ])
        };

        if (relationship.ParentKind is ParentKind.Adopted or ParentKind.Legal)
        {
            path.StrokeDashArray = [5, 3];
        }

        targetCanvas.Children.Add(path);
        return new Point((start.X + end.X) / 2, midY - 22);
    }

    private Point DrawMarriageLine(Canvas targetCanvas, Rect spouseABounds, Rect spouseBBounds)
    {
        var y = Math.Min(spouseABounds.Top, spouseBBounds.Top) + spouseABounds.Height / 2;
        var startX = spouseABounds.Right < spouseBBounds.Left ? spouseABounds.Right : spouseABounds.Left;
        var endX = spouseABounds.Right < spouseBBounds.Left ? spouseBBounds.Left : spouseBBounds.Right;

        var line = new Line
        {
            X1 = startX,
            Y1 = y,
            X2 = endX,
            Y2 = y,
            Stroke = new SolidColorBrush(Palette.MarriageLine),
            StrokeThickness = 3.2,
            StrokeDashArray = [8, 4]
        };

        targetCanvas.Children.Add(line);
        return new Point((startX + endX) / 2, y - 28);
    }

    private static void DrawRelationshipLabel(Canvas targetCanvas, Relationship relationship, Point labelPoint)
    {
        if (string.IsNullOrWhiteSpace(relationship.DisplayLabel))
        {
            return;
        }

        var label = new Border
        {
            MaxWidth = 150,
            Background = new SolidColorBrush(Palette.RelLabelBackground),
            BorderBrush = relationship.Type == RelationshipType.Marriage
                ? new SolidColorBrush(Palette.MarriageLine)
                : new SolidColorBrush(Palette.RelLabelBorder),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(5),
            Padding = new Thickness(6, 2, 6, 2),
            Child = new TextBlock
            {
                Text = relationship.DisplayLabel,
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Palette.RelLabelText),
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center
            }
        };

        label.Measure(new Size(150, double.PositiveInfinity));
        Canvas.SetLeft(label, labelPoint.X - label.DesiredSize.Width / 2);
        Canvas.SetTop(label, labelPoint.Y);
        targetCanvas.Children.Add(label);
    }

    private void DrawPersonCards(Canvas targetCanvas, TreeProject project, TreeLayoutResult layout, bool includeCardInteraction)
    {
        foreach (var person in project.People)
        {
            if (!layout.PersonBounds.TryGetValue(person.Id, out var bounds))
            {
                continue;
            }

            var border = new Border
            {
                Width = bounds.Width,
                Height = bounds.Height,
                Background = GetCardBrush(person.CardType),
                BorderBrush = SelectedPerson?.Id == person.Id ? new SolidColorBrush(Palette.SelectedCardBorder) : GetCardBorderBrush(person.CardType),
                BorderThickness = SelectedPerson?.Id == person.Id ? new Thickness(4) : new Thickness(1.6),
                CornerRadius = person.CardType == CardType.GroupedPeople ? new CornerRadius(8) : new CornerRadius(12),
                Padding = new Thickness(12, 10, 12, 10),
                Tag = person,
                Cursor = includeCardInteraction ? Cursors.Hand : Cursors.Arrow,
                ToolTip = person.EffectiveDisplayName,
                Effect = new DropShadowEffect
                {
                    Color = Color.FromRgb(68, 58, 44),
                    BlurRadius = SelectedPerson?.Id == person.Id ? 18 : 12,
                    ShadowDepth = SelectedPerson?.Id == person.Id ? 3 : 2,
                    Opacity = SelectedPerson?.Id == person.Id ? 0.32 : 0.18
                }
            };

            if (person.CardType == CardType.Unknown)
            {
                border.BorderBrush = SelectedPerson?.Id == person.Id
                    ? new SolidColorBrush(Palette.SelectedCardBorder)
                    : new SolidColorBrush(Palette.CardUnknownBorder);
            }

            var panel = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center
            };
            panel.Children.Add(new TextBlock
            {
                Text = person.EffectiveDisplayName,
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
                Foreground = new SolidColorBrush(Palette.CardTitleText),
                MaxHeight = 40
            });

            panel.Children.Add(new TextBlock
            {
                Text = GetCardSubtitle(person.CardType),
                FontSize = 11,
                TextAlignment = TextAlignment.Center,
                Foreground = new SolidColorBrush(Palette.CardSubtitleText),
                Opacity = 0.78,
                Margin = new Thickness(0, 7, 0, 0)
            });

            border.Child = panel;

            if (includeCardInteraction)
            {
                border.MouseLeftButtonDown += PersonCard_MouseLeftButtonDown;
            }

            Canvas.SetLeft(border, bounds.Left);
            Canvas.SetTop(border, bounds.Top);
            targetCanvas.Children.Add(border);
        }
    }

    private static SaveFileDialog CreateSaveDialog(string defaultFileName)
    {
        return new SaveFileDialog
        {
            Title = "Export Bible Family Tree",
            Filter = "PNG image (*.png)|*.png|All files (*.*)|*.*",
            FileName = defaultFileName,
            AddExtension = true,
            DefaultExt = ".png"
        };
    }

    private static void SaveBitmap(BitmapSource bitmap, string filePath)
    {
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));

        using var stream = File.Create(filePath);
        encoder.Save(stream);
    }

    private static void ShowExportError(Exception ex)
    {
        MessageBox.Show(
            Application.Current.MainWindow,
            $"The tree image could not be exported.{Environment.NewLine}{Environment.NewLine}{ex.Message}",
            "Export failed",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }

    private FrameworkElement CreateExportHeader(double exportWidth)
    {
        var panel = new StackPanel
        {
            Width = Math.Max(320, exportWidth - FullTreeExportPadding * 2 - ExportLegendWidth - 24),
            Orientation = Orientation.Vertical
        };

        panel.Children.Add(new TextBlock
        {
            Text = "Bible Family Tree",
            FontSize = 24,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Palette.TextPrimary)
        });

        panel.Children.Add(new TextBlock
        {
            Text = string.IsNullOrWhiteSpace(ExportTitle) ? "Full Tree" : ExportTitle,
            FontSize = 14,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Palette.TextSecondary),
            Margin = new Thickness(0, 8, 0, 0),
            TextWrapping = TextWrapping.Wrap
        });

        return panel;
    }

    private static Border CreateLegendPanel(bool includeShadow)
    {
        var panel = new StackPanel
        {
            Width = 250
        };

        panel.Children.Add(new TextBlock
        {
            Text = "Legend",
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Palette.TextPrimary),
            Margin = new Thickness(0, 0, 0, 8)
        });

        panel.Children.Add(CreateLegendSwatch("Green = Default", new SolidColorBrush(Palette.CardDefaultFill), new SolidColorBrush(Palette.CardDefaultBorder)));
        panel.Children.Add(CreateLegendSwatch("Yellow = Jesus Line", new SolidColorBrush(Palette.CardJesusFill), new SolidColorBrush(Palette.CardJesusBorder)));
        panel.Children.Add(CreateLegendSwatch("Blue = Unknown Descendants", new SolidColorBrush(Palette.CardUnknownDescFill), new SolidColorBrush(Palette.CardUnknownDescBorder)));
        panel.Children.Add(CreateLegendLine("Red/pink = Marriage", new SolidColorBrush(Palette.MarriageLine), [6, 3]));
        panel.Children.Add(CreateLegendLine("Dashed = Adopted/Legal parent", new SolidColorBrush(Palette.AdoptedLegalLine), [4, 3]));

        var border = new Border
        {
            Width = ExportLegendWidth,
            Height = ExportLegendHeight,
            Padding = new Thickness(12),
            Background = new SolidColorBrush(Palette.PanelBackground),
            BorderBrush = new SolidColorBrush(Palette.PanelBorder),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Child = panel
        };

        if (includeShadow)
        {
            border.Effect = new DropShadowEffect
            {
                BlurRadius = 16,
                ShadowDepth = 2,
                Opacity = 0.16,
                Color = Color.FromRgb(68, 58, 47)
            };
        }

        return border;
    }

    private static FrameworkElement CreateLegendSwatch(string text, Brush fill, Brush stroke)
    {
        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 0, 0, 5)
        };

        row.Children.Add(new Rectangle
        {
            Width = 18,
            Height = 12,
            Fill = fill,
            Stroke = stroke,
            RadiusX = 3,
            RadiusY = 3
        });

        row.Children.Add(new TextBlock
        {
            Text = text,
            Margin = new Thickness(8, 0, 0, 0),
            Foreground = new SolidColorBrush(Palette.TextPrimary)
        });

        return row;
    }

    private static FrameworkElement CreateLegendLine(string text, Brush stroke, DoubleCollection dashArray)
    {
        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 0, 0, 5)
        };

        row.Children.Add(new Line
        {
            X1 = 0,
            Y1 = 7,
            X2 = 22,
            Y2 = 7,
            Stroke = stroke,
            StrokeThickness = 3,
            StrokeDashArray = dashArray
        });

        row.Children.Add(new TextBlock
        {
            Text = text,
            Margin = new Thickness(8, 0, 0, 0),
            Foreground = new SolidColorBrush(Palette.TextPrimary)
        });

        return row;
    }

    private static Brush GetCardBrush(CardType cardType)
    {
        return cardType switch
        {
            CardType.JesusLine => new SolidColorBrush(Palette.CardJesusFill),
            CardType.Unknown => new SolidColorBrush(Palette.CardUnknownFill),
            CardType.UnknownDescendant => new SolidColorBrush(Palette.CardUnknownDescFill),
            CardType.GroupedPeople => new SolidColorBrush(Palette.CardGroupedFill),
            _ => new SolidColorBrush(Palette.CardDefaultFill)
        };
    }

    private static Brush GetCardBorderBrush(CardType cardType)
    {
        return cardType switch
        {
            CardType.JesusLine => new SolidColorBrush(Palette.CardJesusBorder),
            CardType.Unknown => new SolidColorBrush(Palette.CardUnknownBorder),
            CardType.UnknownDescendant => new SolidColorBrush(Palette.CardUnknownDescBorder),
            CardType.GroupedPeople => new SolidColorBrush(Palette.CardGroupedBorder),
            _ => new SolidColorBrush(Palette.CardDefaultBorder)
        };
    }

    private static string GetCardSubtitle(CardType cardType)
    {
        return cardType switch
        {
            CardType.JesusLine => "Jesus Line",
            CardType.UnknownDescendant => "Unknown descendants",
            CardType.GroupedPeople => "Grouped people",
            CardType.Unknown => "Unknown person",
            _ => "Person"
        };
    }

    private void PersonCard_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border { Tag: Person person })
        {
            SelectedPerson = person;
            e.Handled = true;
        }
    }

    private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left && e.Source == PART_Canvas)
        {
            SelectedPerson = null;
        }

        if (e.ChangedButton == MouseButton.Middle || e.RightButton == MouseButtonState.Pressed)
        {
            _lastPanPoint = e.GetPosition(this);
            _isPanning = true;
            PART_Canvas.CaptureMouse();
        }
    }

    private void Canvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isPanning)
        {
            return;
        }

        var currentPoint = e.GetPosition(this);
        var delta = currentPoint - _lastPanPoint;
        PanTransform.X += delta.X;
        PanTransform.Y += delta.Y;
        _lastPanPoint = currentPoint;
    }

    private void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
    {
        _isPanning = false;
        PART_Canvas.ReleaseMouseCapture();
    }

    private void Canvas_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        Zoom = e.Delta > 0
            ? Math.Min(2.5, Zoom + 0.1)
            : Math.Max(0.35, Zoom - 0.1);
    }
}
