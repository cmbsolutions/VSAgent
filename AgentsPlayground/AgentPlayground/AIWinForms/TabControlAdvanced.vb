Imports System.Drawing.Drawing2D
Imports System.ComponentModel

Partial Class TabControlAdvanced
    Inherits Control

    Public Event TabChanged As EventHandler

    Private _tabs As New List(Of TabPageAdvanced)
    Private _selectedIndex As Integer = -1
    Private _tabHeight As Integer = 32
    Private _tabPadding As Integer = 16
    Private _hoveredTabIndex As Integer = -1
    Private _activeTabRects As List(Of Rectangle) = New List(Of Rectangle)
    Private _contentTop As Integer = 0

    ' ─── Color Palette ───────────────────────────────────────────────
    Private _tabNormalColor As Color = Color.FromArgb(45, 45, 48)
    Private _tabActiveColor As Color = Color.FromArgb(255, 255, 255)
    Private _tabHoverColor As Color = Color.FromArgb(60, 60, 65)
    Private _tabNormalText As Color = Color.FromArgb(180, 180, 185)
    Private _tabActiveText As Color = Color.FromArgb(30, 30, 30)
    Private _contentBg As Color = Color.FromArgb(32, 32, 36)
    Private _indicatorColor As Color = Color.FromArgb(100, 120, 255)

    ' ─── Public Properties ───────────────────────────────────────────
    <DefaultValue(32)>
    Public Property TabHeight As Integer
        Get
            Return _tabHeight
        End Get
        Set(value As Integer)
            _tabHeight = Math.Max(24, value)
            Invalidate()
        End Set
    End Property

    <DefaultValue(-1)>
    Public Property SelectedIndex As Integer
        Get
            Return _selectedIndex
        End Get
        Set(value As Integer)
            If value < -1 OrElse value >= _tabs.Count Then
                Throw New ArgumentOutOfRangeException(NameOf(value))
            End If
            _selectedIndex = value
            RecreateContentPanel()
            Invalidate()
            RaiseEvent TabChanged(Me, EventArgs.Empty)
        End Set
    End Property

    Public ReadOnly Property Tabs As IReadOnlyList(Of TabPageAdvanced)
        Get
            Return _tabs
        End Get
    End Property

    ' ─── Tab Management ──────────────────────────────────────────────
    Public Sub AddTab(ByVal name As String, ByVal content As Control)
        Dim page As New TabPageAdvanced(name, content)
        _tabs.Add(page)
        If _selectedIndex < 0 Then _selectedIndex = 0
        RecreateContentPanel()
        Invalidate()
    End Sub

    Public Sub AddTab(ByVal name As String)
        AddTab(name, New Panel())
    End Sub

    Public Sub RemoveAt(ByVal index As Integer)
        If index < 0 OrElse index >= _tabs.Count Then Return
        _tabs(index).Dispose()
        _tabs.RemoveAt(index)
        If _selectedIndex >= _tabs.Count Then _selectedIndex = _tabs.Count - 1
        RecreateContentPanel()
        Invalidate()
    End Sub

    ' ─── Constructor ─────────────────────────────────────────────────
    Public Sub New()
        InitializeComponent()

        SetStyle(ControlStyles.AllPaintingInWmPaint Or
                 ControlStyles.UserPaint Or
                 ControlStyles.OptimizedDoubleBuffer Or
                 ControlStyles.ResizeRedraw, True)
        DoubleBuffered = True
        Font = New Font("Segoe UI", 9.5F)
        BackColor = _contentBg
    End Sub

    ' ─── Content Panel Management ────────────────────────────────────
    Private Sub RecreateContentPanel()
        ' Remove all children except the tab strip area
        For Each c As Control In Controls
            c.Dispose()
        Next
        Controls.Clear()

        _contentTop = _tabHeight + 4

        If _selectedIndex < 0 OrElse _selectedIndex >= _tabs.Count Then Return

        Dim content As Control = _tabs(_selectedIndex).Content
        content.Top = _contentTop
        content.Left = 0
        content.Width = Me.Width
        content.Height = Me.Height - _contentTop
        content.Visible = True
        Controls.Add(content)
    End Sub

    ' ─── Painting ────────────────────────────────────────────────────
    Protected Overrides Sub OnPaint(ByVal e As PaintEventArgs)
        Dim g As Graphics = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit

        ' Fill background
        g.FillRectangle(Brushes.Black, ClientRectangle)

        _activeTabRects.Clear()

        ' Draw tabs
        Dim x As Integer = 0
        Dim measureFont As Font = Font

        For i As Integer = 0 To _tabs.Count - 1
            Dim textSize As SizeF = g.MeasureString(_tabs(i).TabName, measureFont)
            Dim tabWidth As Integer = CInt(textSize.Width) + _tabPadding * 2
            Dim tabRect As New Rectangle(x, 2, tabWidth, _tabHeight - 2)
            _activeTabRects.Add(tabRect)

            If i = _selectedIndex Then
                ' Active tab – white rounded rectangle
                DrawRoundedRect(g, tabRect, 8, _tabActiveColor, Nothing)
                g.DrawString(_tabs(i).TabName, measureFont,
                             New SolidBrush(_tabActiveText),
                             tabRect.X + _tabPadding,
                             tabRect.Y + (tabRect.Height - textSize.Height) / 2)

                ' Indicator bar
                Dim barRect As New Rectangle(tabRect.X + 12, tabRect.Bottom - 3,
                                             tabRect.Width - 24, 3)
                g.FillRectangle(New SolidBrush(_indicatorColor), barRect)

            ElseIf i = _hoveredTabIndex Then
                DrawRoundedRect(g, tabRect, 8, _tabHoverColor, Nothing)
                g.DrawString(_tabs(i).TabName, measureFont,
                             New SolidBrush(_tabNormalText),
                             tabRect.X + _tabPadding,
                             tabRect.Y + (tabRect.Height - textSize.Height) / 2)
            Else
                DrawRoundedRect(g, tabRect, 8, _tabNormalColor, Nothing)
                g.DrawString(_tabs(i).TabName, measureFont,
                             New SolidBrush(_tabNormalText),
                             tabRect.X + _tabPadding,
                             tabRect.Y + (tabRect.Height - textSize.Height) / 2)
            End If

            x += tabWidth + 4
        Next

        ' Content area border (subtle)
        Dim contentRect As New Rectangle(0, _contentTop, Me.Width, Me.Height - _contentTop)
        g.DrawRectangle(Pens.Gray, contentRect)
    End Sub

    Private Shared Function DrawRoundedRect(g As Graphics, rect As Rectangle,
                                           ByVal radius As Integer,
                                           ByVal fill As Color,
                                           ByVal border As Color?) As GraphicsPath
        Dim path As New GraphicsPath()
        Dim d As Integer = radius * 2

        path.AddArc(rect.X, rect.Y, d, d, 180, 90)
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90)
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90)
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90)
        path.CloseFigure()

        If fill <> Color.Transparent Then
            g.FillPath(New SolidBrush(fill), path)
        End If
        If border.HasValue Then
            g.DrawPath(New Pen(border.Value, 1), path)
        End If
        Return path
    End Function

    ' ─── Mouse Handling ──────────────────────────────────────────────
    Protected Overrides Sub OnMouseMove(ByVal e As MouseEventArgs)
        MyBase.OnMouseMove(e)
        Dim newIndex As Integer = HitTestTab(e.Location)
        If newIndex <> _hoveredTabIndex Then
            _hoveredTabIndex = newIndex
            Invalidate()
        End If
    End Sub

    Protected Overrides Sub OnMouseLeave(ByVal e As EventArgs)
        MyBase.OnMouseLeave(e)
        _hoveredTabIndex = -1
        Invalidate()
    End Sub

    Protected Overrides Sub OnMouseDown(ByVal e As MouseEventArgs)
        MyBase.OnMouseDown(e)
        Dim idx As Integer = HitTestTab(e.Location)
        If idx >= 0 AndAlso idx <> _selectedIndex Then
            _selectedIndex = idx
            RecreateContentPanel()
            Invalidate()
            RaiseEvent TabChanged(Me, EventArgs.Empty)
        End If
    End Sub

    Private Function HitTestTab(ByVal point As Point) As Integer
        For i As Integer = 0 To _activeTabRects.Count - 1
            If _activeTabRects(i).Contains(point) Then Return i
        Next
        Return -1
    End Function

    Protected Overrides Sub OnResize(ByVal e As EventArgs)
        MyBase.OnResize(e)
        RecreateContentPanel()
    End Sub
End Class

Public Class TabPageAdvanced
    Implements IDisposable

    Private _name As String
    Private _content As Control
    Private _disposed As Boolean

    Public ReadOnly Property TabName As String
        Get
            Return _name
        End Get
    End Property

    Public ReadOnly Property Content As Control
        Get
            Return _content
        End Get
    End Property

    Public Sub New(ByVal name As String, ByVal content As Control)
        _name = name
        _content = content
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        If Not _disposed Then
            _content?.Dispose()
            _disposed = True
        End If
        GC.SuppressFinalize(Me)
    End Sub
End Class
