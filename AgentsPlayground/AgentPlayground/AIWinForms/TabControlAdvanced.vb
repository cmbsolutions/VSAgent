Imports System.ComponentModel
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms

Public Class TabControlAdvanced
    Inherits ContainerControl

    Public Event TabChanged As EventHandler

    Private _selectedIndex As Integer = -1
    Private _tabHeight As Integer = 32
    Private _tabPadding As Integer = 16
    Private _hoveredTabIndex As Integer = -1
    Private _activeTabRects As List(Of Rectangle) = New List(Of Rectangle)

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
            LayoutTabs()
            Invalidate()
        End Set
    End Property

    <Browsable(False), DefaultValue(-1)>
    Public Property SelectedIndex As Integer
        Get
            Return _selectedIndex
        End Get
        Set(value As Integer)
            If value < -1 OrElse value >= TabPages.Count Then
                Throw New ArgumentOutOfRangeException(NameOf(value))
            End If
            _selectedIndex = value
            LayoutTabs()
            Invalidate()
            RaiseEvent TabChanged(Me, EventArgs.Empty)
        End Set
    End Property

    ' ─── Tab Pages Collection ────────────────────────────────────────
    Public ReadOnly Property TabPages As List(Of TabPageAdvanced)
        Get
            Return Controls.OfType(Of TabPageAdvanced).ToList()
        End Get
    End Property

    ' ─── Tab Management ──────────────────────────────────────────────
    Public Sub AddTab(ByVal name As String)
        Dim page As New TabPageAdvanced()
        page.Name = name
        Controls.Add(page)
        If _selectedIndex < 0 Then _selectedIndex = 0
        LayoutTabs()
        Invalidate()
    End Sub

    Public Sub RemoveAt(ByVal index As Integer)
        Dim pages = TabPages
        If index < 0 OrElse index >= pages.Count Then Return
        Controls.Remove(pages(index))
        pages(index).Dispose()
        If _selectedIndex >= TabPages.Count Then _selectedIndex = TabPages.Count - 1
        LayoutTabs()
        Invalidate()
    End Sub

    ' ─── Designer Support ────────────────────────────────────────────
    Protected Overrides Sub OnControlAdded(ByVal e As ControlEventArgs)
        MyBase.OnControlAdded(e)
        If TypeOf e.Control Is TabPageAdvanced Then
            If _selectedIndex < 0 Then _selectedIndex = 0
            LayoutTabs()
            Invalidate()
        End If
    End Sub

    Protected Overrides Sub OnControlRemoved(ByVal e As ControlEventArgs)
        MyBase.OnControlRemoved(e)
        If TypeOf e.Control Is TabPageAdvanced Then
            If _selectedIndex >= TabPages.Count Then _selectedIndex = TabPages.Count - 1
            LayoutTabs()
            Invalidate()
        End If
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
        Me.Size = New Size(300, 200)
    End Sub

    ' ─── Layout ──────────────────────────────────────────────────────
    Private Sub LayoutTabs()
        Dim contentTop As Integer = _tabHeight + 4
        For Each page As TabPageAdvanced In Controls.OfType(Of TabPageAdvanced)
            Dim idx As Integer = TabPages.IndexOf(page)
            Dim isSelected As Boolean = (idx = _selectedIndex)
            page.Top = contentTop
            page.Left = 0
            page.Width = Me.Width
            page.Height = Me.Height - contentTop
            page.Visible = isSelected
        Next
    End Sub

    ' ─── Painting ────────────────────────────────────────────────────
    Protected Overrides Sub OnPaint(ByVal e As PaintEventArgs)
        Dim g As Graphics = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit

        ' Fill background
        g.FillRectangle(New SolidBrush(_contentBg), ClientRectangle)

        _activeTabRects.Clear()

        ' Draw tabs
        Dim pages = TabPages
        Dim x As Integer = 0
        Dim measureFont As Font = Font

        For i As Integer = 0 To pages.Count - 1
            Dim page = pages(i)
            Dim tabName As String = If(String.IsNullOrEmpty(page.Name) AndAlso page.Site IsNot Nothing, page.Site.Name, page.Name)
            If String.IsNullOrEmpty(tabName) Then tabName = "Tab" & (i + 1)

            Dim textSize As SizeF = g.MeasureString(tabName, measureFont)
            Dim tabWidth As Integer = CInt(textSize.Width) + _tabPadding * 2
            Dim tabRect As New Rectangle(x, 2, tabWidth, _tabHeight - 2)
            _activeTabRects.Add(tabRect)

            If i = _selectedIndex Then
                ' Active tab – white rounded rectangle
                DrawRoundedRect(g, tabRect, 8, _tabActiveColor, Nothing)
                g.DrawString(tabName, measureFont,
                             New SolidBrush(_tabActiveText),
                             tabRect.X + _tabPadding,
                             tabRect.Y + (tabRect.Height - textSize.Height) / 2)

                ' Indicator bar
                Dim barRect As New Rectangle(tabRect.X + 12, tabRect.Bottom - 3,
                                             tabRect.Width - 24, 3)
                g.FillRectangle(New SolidBrush(_indicatorColor), barRect)

            ElseIf i = _hoveredTabIndex Then
                DrawRoundedRect(g, tabRect, 8, _tabHoverColor, Nothing)
                g.DrawString(tabName, measureFont,
                             New SolidBrush(_tabNormalText),
                             tabRect.X + _tabPadding,
                             tabRect.Y + (tabRect.Height - textSize.Height) / 2)
            Else
                DrawRoundedRect(g, tabRect, 8, _tabNormalColor, Nothing)
                g.DrawString(tabName, measureFont,
                             New SolidBrush(_tabNormalText),
                             tabRect.X + _tabPadding,
                             tabRect.Y + (tabRect.Height - textSize.Height) / 2)
            End If

            x += tabWidth + 4
        Next

        ' Content area border (subtle)
        Dim contentRect As New Rectangle(0, _tabHeight + 4, Me.Width, Me.Height - _tabHeight - 4)
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
            LayoutTabs()
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
        LayoutTabs()
    End Sub
End Class

Public Class TabPageAdvanced
    Inherits System.Windows.Forms.Control

    <Browsable(False)>
    Public ReadOnly Property Content As Control
        Get
            Return DirectCast(Me.Tag, Control)
        End Get
    End Property

    <Browsable(False), DefaultValue("")>
    Public Property TabName As String
        Get
            Return Name
        End Get
        Set(value As String)
            Name = value
            Invalidate()
        End Set
    End Property

    Public Sub New()
        TabStop = False
        BackColor = Color.White
    End Sub

    Public Sub New(ByVal name As String, ByVal content As Control)
        Me.New()
        Name = name
        Tag = content
    End Sub
End Class
