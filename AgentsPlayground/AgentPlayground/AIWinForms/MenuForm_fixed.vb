Imports System.Drawing.Drawing2D
Imports System.Reflection

''' <summary>
''' Main menu form that allows the user to navigate to other application forms.
''' </summary>
Public Class MenuForm
    Inherits Form

    ' Animation timer and state
    Private WithEvents animTimer As New Timer() With {.Interval = 16, .Enabled = True}
    Private tickCounter As Integer = 0
    Private particles As List(Of Particle) = New List(Of Particle)()
    Private bgPanel As Panel = Nothing
    Private _paintLock As Object = New Object()
    Private _openedForms As HashSet(Of Form) = New HashSet(Of Form)()

    Public Sub New()
        Me.SuspendLayout()
        InitializeComponent()
        Me.ResumeLayout(False)
    End Sub

    Protected Overrides Sub OnFormClosed(e As FormClosedEventArgs)
        MyBase.OnFormClosed(e)
        animTimer.Stop()
        animTimer.Dispose()
    End Sub

    Private Sub SpawnParticlesAtCursor(accentColor As Color, count As Integer)
        If Not Me.Visible AndAlso Not Me.IsHandleCreated Then Return
        
        Dim screenPos As Point = Cursor.Position
        Dim clientPos As Point = Me.PointToClient(screenPos)
        
        SyncLock _paintLock
            For j As Integer = 0 To count - 1
                particles.Add(New Particle(
                    CSng(clientPos.X + Random.Shared.Next(-6, 7)),
                    CSng(clientPos.Y + Random.Shared.Next(-6, 7)),
                    accentColor))
            Next
        End SyncLock
    End Sub

    Private Sub InitializeComponent()
        ' ====== Main form settings ======
        Me.Text = "╳ AIWinForms v∞"
        Me.Size = New Size(640, 480)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.MinimumSize = New Size(580, 380)
        Me.BackColor = Color.FromArgb(8, 12, 24)
        Me.FormBorderStyle = FormBorderStyle.None
        Me.DoubleBuffered = True
        Me.AllowDrop = True

        ' ====== Background Panel (draws grid, radial gradient, and particles) ======
        bgPanel = New Panel() With {
            .Name = "bgLayer",
            .Dock = DockStyle.Fill,
            .BackColor = Color.FromArgb(8, 12, 24)
        }
        AddHandler bgPanel.Paint, AddressOf GlassPanel_Paint
        Me.Controls.Add(bgPanel)

        ' ====== Top Accent Bar (with animated gradient) ======
        Dim topBar As New Panel() With {
            .Name = "topAccentBar",
            .Dock = DockStyle.Top,
            .Height = 3,
            .BackColor = Color.FromArgb(0, 229, 255)
        }
        AddHandler topBar.Paint, AddressOf TopBar_Paint
        Me.Controls.Add(topBar)

        ' ====== Title Section with animated gradient ======
        Dim titlePanel As New Panel() With {
            .Name = "titleSection",
            .Size = New Size(600, 100),
            .Location = New Point(20, 15),
            .BackColor = Color.FromArgb(25, 38, 70)
        }
        AddHandler titlePanel.Paint, AddressOf TitlePanel_Paint

        Dim mainTitleLabel As New Label() With {
            .Name = "mainTitle",
            .Text = "AIWinForms",
            .Font = New Font("Segoe UI", 32.0F, FontStyle.Bold, GraphicsUnit.Point),
            .Location = New Point(150, 15),
            .AutoSize = False,
            .Size = New Size(300, 50),
            .ForeColor = Color.FromArgb(0, 229, 255),
            .TextAlign = ContentAlignment.MiddleCenter,
            .BackColor = Color.Transparent
        }
        titlePanel.Controls.Add(mainTitleLabel)

        Dim subtitleLabel As New Label() With {
            .Name = "subtitle",
            .Text = "QUANTUM NAVIGATION SYSTEM",
            .Font = New Font("Consolas", 9.0F, FontStyle.Bold),
            .Location = New Point(150, 60),
            .AutoSize = False,
            .Size = New Size(300, 20),
            .ForeColor = Color.FromArgb(100, 200, 255),
            .TextAlign = ContentAlignment.MiddleCenter,
            .BackColor = Color.Transparent
        }
        titlePanel.Controls.Add(subtitleLabel)

        ' ====== Menu Items Container ======
        Dim menuContainer As New Panel() With {
            .Name = "menuItems",
            .Size = New Size(600, 280),
            .Location = New Point(20, 115),
            .BackColor = Color.Transparent
        }

        ' ====== Define menu items ======
        Dim menuDefs As List(Of MenuItemDef) = CreateMenuDefinitions()

        For i As Integer = 0 To menuDefs.Count - 1
            Dim def = menuDefs(i)
            Dim menuItemPanel = CreateMenuItemPanel(def)
            
            ' Position each menu item vertically
            menuItemPanel.Location = New Point(20, i * 85 + 5)
            menuContainer.Controls.Add(menuItemPanel)
        Next

        ' ====== Bottom Status Bar ======
        Dim statusBar As New Panel() With {
            .Name = "statusBar",
            .Dock = DockStyle.Bottom,
            .Height = 30,
            .BackColor = Color.Transparent
        }
        Me.Controls.Add(statusBar)

        ' Add all controls to form
        bgPanel.Controls.Add(titlePanel)
        bgPanel.Controls.Add(menuContainer)

        ' ====== Close button (top-right corner) ======
        Dim closeButton As New Label() With {
            .Name = "closeButton",
            .Text = ChrW(215),           ' × character displayed clearly
            .Font = New Font("Segoe UI", 24.0F, FontStyle.Bold),
            .Size = New Size(45, 45),
            .Location = New Point(CInt(Me.Width) - 45, 0),
            .ForeColor = Color.FromArgb(180, 200, 220),
            .BackColor = Color.FromArgb(18, 24, 38),
            .Cursor = Cursors.Hand,
            .TextAlign = ContentAlignment.MiddleCenter,
            .TabIndex = 0,
            .Visible = True
        }
        AddHandler closeButton.MouseEnter, Sub(s, e) DirectCast(s, Label).ForeColor = Color.FromArgb(255, 80, 80)
        AddHandler closeButton.MouseLeave, Sub(s, e) DirectCast(s, Label).ForeColor = Color.FromArgb(180, 200, 220)
        AddHandler closeButton.Click, AddressOf CloseButton_Click
        Me.Controls.Add(closeButton)

        ' ====== Status bar text ======
        Dim statusBarLabel As New Label() With {
            .Name = "statusBarText",
            .Text = "Quantum Navigation System v∞",
            .Font = New Font("Consolas", 8.0F),
            .Size = New Size(400, 30),
            .Location = New Point(10, 5),
            .ForeColor = Color.FromArgb(60, 100, 140),
            .BackColor = Color.Transparent,
            .TextAlign = ContentAlignment.MiddleCenter
        }
        statusBar.Controls.Add(statusBarLabel)
    End Sub

    Private Function CreateMenuDefinitions() As List(Of MenuItemDef)
        Return New List(Of MenuItemDef) From {
            New MenuItemDef With {
                .Name = "btn_OpenAIClientForm",
                .Title = "Neural Network Explorer",
                .Description = "Connect to OpenAI's advanced language models for AI-powered interactions",
                .TargetType = GetType(OpenAIClientForm),
                .IconChar = ChrW(9679),   ' ◉ (filled circle)
                .GradientStart = Color.FromArgb(0, 180, 255),
                .GradientEnd = Color.FromArgb(147, 60, 255)
            },
            New MenuItemDef With {
                .Name = "btn_JSONParser",
                .Title = "Data Structure Analyzer",
                .Description = "Visualize and parse JSON data with interactive hierarchical tree view",
                .TargetType = GetType(Form1),
                .IconChar = ChrW(9678),   ' ◈ (circled diamond)
                .GradientStart = Color.FromArgb(32, 145, 210),
                .GradientEnd = Color.FromArgb(69, 200, 255)
            }
        }
    End Function

    Private Function CreateMenuItemPanel(def As MenuItemDef) As Panel
        ' --- Main panel (clickable region) ---
        Dim menuItemPanel As New Panel() With {
            .Name = def.Name,
            .Size = New Size(560, 70),
            .Location = New Point(20, 0),
            .BackColor = Color.FromArgb(18, 24, 38),
            .BorderStyle = BorderStyle.None,
            .Cursor = Cursors.Hand
        }

        ' --- Glowing border ---
        Dim glowBorder As New Panel() With {
            .Name = $"glow_{def.Name}",
            .Size = New Size(562, 72),
            .Location = New Point(-1, -1),
            .BackColor = Color.Transparent
        }
        AddHandler glowBorder.Paint, Sub(s, e) DrawGlowBorder(e, def.GradientStart)
        menuItemPanel.Controls.Add(glowBorder)

        ' --- Accent gradient bar on left ---
        Dim accentBar As New Panel() With {
            .Name = $"accent_{def.Name}",
            .Size = New Size(4, 50),
            .Location = New Point(12, 10),
            .BackColor = Color.Transparent
        }
        AddHandler accentBar.Paint, Sub(s, e) DrawGradientBar(e, def.GradientStart, def.GradientEnd)
        menuItemPanel.Controls.Add(accentBar)

        ' --- Icon background (filled circle) ---
        Dim iconBg As New Panel() With {
            .Name = $"iconBg_{def.Name}",
            .Size = New Size(36, 36),
            .Location = New Point(28, 17),
            .BackColor = Color.FromArgb(10, 15, 28)
        }
        AddHandler iconBg.Paint, Sub(s, e) DrawGradientIcon(e, def.GradientStart, def.GradientEnd)
        menuItemPanel.Controls.Add(iconBg)

        ' --- Icon label ---
        Dim iconLabel As New Label() With {
            .Name = $"icon_{def.Name}",
            .Text = def.IconChar.ToString(),
            .Font = New Font("Segoe UI Emoji", 16.0F),
            .Location = New Point(30, 19),
            .AutoSize = False,
            .Size = New Size(32, 32),
            .ForeColor = Color.FromArgb(255, 255, 255),
            .TextAlign = ContentAlignment.MiddleCenter,
            .BackColor = Color.Transparent
        }

        ' --- Title label (visible text for the button) ---
        Dim titleLabel As New Label() With {
            .Name = $"title_{def.Name}",
            .Text = def.Title,
            .Font = New Font("Segoe UI", 13.0F, FontStyle.Bold, GraphicsUnit.Point),
            .AutoSize = False,
            .Size = New Size(480, 24),
            .Location = New Point(76, 12),
            .ForeColor = Color.FromArgb(255, 255, 255),
            .TextAlign = ContentAlignment.MiddleLeft,
            .BackColor = Color.Transparent
        }

        ' --- Description label ---
        Dim descLabel As New Label() With {
            .Name = $"desc_{def.Name}",
            .Text = def.Description,
            .Font = New Font("Segoe UI", 9.0F, FontStyle.Regular, GraphicsUnit.Point),
            .AutoSize = False,
            .Size = New Size(480, 30),
            .Location = New Point(76, 38),
            .ForeColor = Color.FromArgb(120, 140, 180),
            .TextAlign = ContentAlignment.MiddleLeft,
            .BackColor = Color.Transparent
        }

        ' --- Arrow indicator panel ---
        Dim arrowPanel As New Panel() With {
            .Name = $"arrow_{def.Name}",
            .Size = New Size(40, 36),
            .Location = New Point(510, 17),
            .BackColor = Color.Transparent
        }
        AddHandler arrowPanel.Paint, Sub(s, e) DrawArrow(e, def.GradientStart)

        ' Add all child controls to the panel
        menuItemPanel.Controls.Add(iconBg)
        menuItemPanel.Controls.Add(iconLabel)
        menuItemPanel.Controls.Add(titleLabel)
        menuItemPanel.Controls.Add(descLabel)
        menuItemPanel.Controls.Add(arrowPanel)

        ' --- Shared click handler for entire panel and ALL children ---
        Dim handleItemClick As EventHandler = Sub(s, e) HandleMenuItemClick(def.TargetType)

        ' Wire to every clickable child so clicks are never swallowed by WinForms
        AddHandler menuItemPanel.Click, handleItemClick
        AddHandler accentBar.Click, handleItemClick
        AddHandler iconLabel.Click, handleItemClick
        AddHandler titleLabel.Click, handleItemClick
        AddHandler descLabel.Click, handleItemClick
        AddHandler arrowPanel.Click, handleItemClick

        ' --- Hover effects (back color change + particle burst) ---
        Dim handleHoverEnter As EventHandler = Sub(s, e)
            menuItemPanel.BackColor = Color.FromArgb(25, 33, 50)
            SpawnParticlesAtCursor(def.GradientStart, 5)
        End Sub
        Dim handleHoverLeave As EventHandler = Sub(s, e)
            menuItemPanel.BackColor = Color.FromArgb(18, 24, 38)
        End Sub

        AddHandler menuItemPanel.MouseEnter, handleHoverEnter
        AddHandler menuItemPanel.MouseLeave, handleHoverLeave
        AddHandler accentBar.MouseEnter, handleHoverEnter
        AddHandler accentBar.MouseLeave, handleHoverLeave
        AddHandler iconLabel.MouseEnter, handleHoverEnter
        AddHandler iconLabel.MouseLeave, handleHoverLeave
        AddHandler titleLabel.MouseEnter, handleHoverEnter
        AddHandler titleLabel.MouseLeave, handleHoverLeave
        AddHandler descLabel.MouseEnter, handleHoverEnter
        AddHandler descLabel.MouseLeave, handleHoverLeave
        AddHandler arrowPanel.MouseEnter, handleHoverEnter
        AddHandler arrowPanel.MouseLeave, handleHoverLeave

        Return menuItemPanel
    End Function

    Private Sub DrawGlowBorder(e As PaintEventArgs, accentColor As Color)
        Dim g As Graphics = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        Using pen As New Pen(accentColor, 2.0F)
            pen.EndCap = LineCap.Round
            pen.StartCap = LineCap.Round
            g.DrawRectangle(pen, 0, 0, 561, 71)
        End Using
    End Sub

    Private Sub DrawGradientBar(e As PaintEventArgs, startColor As Color, endColor As Color)
        If e.ClipRectangle.Width <= 0 OrElse e.ClipRectangle.Height <= 0 Then Return
        Dim g As Graphics = e.Graphics
        Using brush As New LinearGradientBrush(e.ClipRectangle, startColor, endColor, LinearGradientMode.Vertical)
            g.FillRectangle(brush, e.ClipRectangle)
        End Using
    End Sub

    Private Sub DrawGradientIcon(e As PaintEventArgs, startColor As Color, endColor As Color)
        If e.ClipRectangle.Width <= 0 OrElse e.ClipRectangle.Height <= 0 Then Return
        Dim g As Graphics = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        Using brush As New LinearGradientBrush(e.ClipRectangle, startColor, endColor, LinearGradientMode.BackwardDiagonal)
            g.FillEllipse(brush, 0, 0, 36, 36)
        End Using
    End Sub

    Private Sub DrawArrow(e As PaintEventArgs, accentColor As Color)
        Dim g As Graphics = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        Using pen As New Pen(accentColor, 2.5F)
            g.DrawLine(pen, 0, 18, 30, 18)
        End Using
        Using brush As New SolidBrush(accentColor)
            g.FillPolygon(brush, {New Point(24, 10), New Point(36, 18), New Point(24, 26)})
        End Using
    End Sub

    Private Sub animTimer_Tick(sender As Object, e As EventArgs) Handles animTimer.Tick
        SyncLock _paintLock
            tickCounter += 1

            ' Update and remove dead particles using index loop (no .ToList() allocation)
            For idx As Integer = particles.Count - 1 To 0 Step -1
                Dim p As Particle = particles(idx)
                p.X += CSng(p.VX)
                p.Y += CSng(p.VY)
                p.Alpha -= 2

                If p.Alpha <= 0 Then
                    particles.RemoveAt(idx)
                End If
            Next

            ' Add ambient particles periodically
            If tickCounter Mod 10 = 0 AndAlso particles.Count < 60 Then
                Dim angle As Double = Random.Shared.NextDouble() * Math.PI * 2
                Dim radius As Single = CSng(Random.Shared.NextDouble() * 200 + 30)
                Dim cx As Single = Me.ClientSize.Width / 2
                Dim cy As Single = Me.ClientSize.Height / 2

                If cx > 0 AndAlso cy > 0 Then
                    particles.Add(New Particle(
                        cx + CSng(Math.Cos(angle) * radius),
                        cy + CSng(Math.Sin(angle) * radius),
                        Color.FromArgb(0, 229, 255)))
                End If
            End If
        End SyncLock

        bgPanel?.Invalidate()
    End Sub

    Private Sub GlassPanel_Paint(sender As Object, e As PaintEventArgs)
        Dim g As Graphics = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias

        If Me.Width <= 0 OrElse Me.Height <= 0 Then Return

        ' Animated grid background
        g.Clear(Color.FromArgb(8, 12, 24))

        Dim gridSize As Integer = 40
        SyncLock _paintLock
            Dim offset As Single = CSng(tickCounter * 0.5) Mod gridSize
            Using pen As New Pen(Color.FromArgb(15, 30, 60), 1)
                For x As Integer = -gridSize + CInt(offset) To Me.Width + gridSize Step gridSize
                    g.DrawLine(pen, x, 0, x, Me.Height)
                Next
                For y As Integer = -gridSize + CInt(offset) To Me.Height + gridSize Step gridSize
                    g.DrawLine(pen, 0, y, Me.Width, y)
                Next
            End Using
        End SyncLock

        ' Radial gradient overlay (concentric circles)
        Dim centerX As Single = CSng(Me.Width / 2)
        Dim centerY As Single = CSng(Me.Height / 2)
        Dim maxRadius As Single = CSng(Math.Max(Me.Width, Me.Height) * 0.7F)
        Dim steps As Integer = 50

        For i As Integer = steps To 0 Step -1
            Dim ratio As Single = CSng(i) / CSng(steps)
            Dim radius As Single = maxRadius * ratio
            Dim alpha As Byte = CByte(CLng(15) * (1 - ratio))
            Using brush As New SolidBrush(Color.FromArgb(alpha, 20, 30, 60))
                g.FillEllipse(brush, centerX - radius, centerY - radius, radius * 2, radius * 2)
            End Using
        Next

        ' Draw particles (synchronized with timer updates)
        SyncLock _paintLock
            For idx As Integer = 0 To particles.Count - 1
                Dim particle = particles(idx)
                If particle.Alpha > 0 Then
                    Using brush As New SolidBrush(Color.FromArgb(particle.Alpha, particle.Color))
                        g.FillEllipse(brush,
                                      particle.X - particle.Size / 2,
                                      particle.Y - particle.Size / 2,
                                      particle.Size, particle.Size)
                    End Using
                End If
            Next
        End SyncLock
    End Sub

    Private Sub TitlePanel_Paint(sender As Object, e As PaintEventArgs)
        Dim g As Graphics = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias

        If e.ClipRectangle.Width <= 0 OrElse e.ClipRectangle.Height <= 0 Then Return

        Using brush As New LinearGradientBrush(e.ClipRectangle,
                                                Color.FromArgb(10, 20, 40),
                                                Color.FromArgb(20, 35, 65),
                                                LinearGradientMode.Horizontal)
            g.FillRectangle(brush, e.ClipRectangle)
        End Using

        ' Animated top border glow
        SyncLock _paintLock
            Dim glowColor As Byte = CByte(100 + 155 * Math.Abs(Math.Sin(tickCounter * 0.02)))
            Using pen As New Pen(Color.FromArgb(glowColor, 0, 229, 255), 2)
                g.DrawLine(pen, 0, 0, CSng(DirectCast(sender, Control).Width), 0)
            End Using
        End SyncLock
    End Sub

    Private Sub TopBar_Paint(sender As Object, e As PaintEventArgs)
        Dim g As Graphics = e.Graphics

        If e.ClipRectangle.Width <= 0 OrElse e.ClipRectangle.Height <= 0 Then Return

        SyncLock _paintLock
            Dim glowColor As Byte = CByte(127 + 128 * Math.Abs(Math.Sin(tickCounter * 0.03)))
            Using brush As New LinearGradientBrush(e.ClipRectangle,
                                                  Color.FromArgb(glowColor, 0, 180, 255),
                                                  Color.FromArgb(glowColor, 147, 60, 255),
                                                  LinearGradientMode.Horizontal)
                g.FillRectangle(brush, e.ClipRectangle)
            End Using
        End SyncLock
    End Sub

    Private Sub HandleMenuItemClick(targetType As Type)
        If targetType Is Nothing Then Return

        Try
            ' Check if an instance is already open and not disposed
            For Each existingForm As Form In _openedForms
                If Not existingForm.IsDisposed AndAlso existingForm.GetType() Is targetType Then
                    If existingForm.WindowState = FormWindowState.Minimized Then
                        existingForm.WindowState = FormWindowState.Normal
                    End If
                    existingForm.Focus()
                    Return
                End If
            Next

            ' No existing instance — create a new one
            Dim instance As Form = CType(Activator.CreateInstance(targetType), Form)
            AddHandler instance.FormClosed, Sub(s, eArg) _openedForms.Remove(instance)
            SyncLock _paintLock
                _openedForms.Add(instance)
            End SyncLock
            instance.Show()

        Catch ex As MissingMethodException
            MessageBox.Show($"Cannot create form ""{targetType.Name}"" — no parameterless constructor found.",
                            "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Debug.WriteLine($"[MenuForm] {ex.GetType().Name}: {ex.Message}")
        Catch ex As TargetInvocationException
            MessageBox.Show($"Error creating form ""{targetType.Name}"": {ex.InnerException?.Message}",
                            "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Debug.WriteLine($"[MenuForm] {ex.GetType().Name}: {ex.Message} — Inner: {ex.InnerException?.Message}")
        Catch ex As Exception
            MessageBox.Show($"Unexpected error opening form ""{targetType.Name}"": {ex.Message}",
                            "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Debug.WriteLine($"[MenuForm] {ex.GetType().Name}: {ex.Message}")
        End Try
    End Sub

    Private Sub CloseButton_Click(sender As Object, e As EventArgs)
        Me.Close()
    End Sub

    Protected Overrides Function ProcessCmdKey(ByRef msg As Message, keyData As Keys) As Boolean
        If keyData = Keys.Escape Then
            Me.Close()
            Return True
        End If
        Return MyBase.ProcessCmdKey(msg, keyData)
    End Function

    Protected Overrides Sub OnShown(e As EventArgs)
        MyBase.OnShown(e)

        ' Reposition close button now that form has real client size
        Dim closeBtn = TryCast(Me.Controls.OfType(Of Label)().FirstOrDefault(Function(l) l.Name = "closeButton"), Label)
        If closeBtn IsNot Nothing Then
            closeBtn.BringToFront()
            closeBtn.Location = New Point(CInt(Me.ClientSize.Width) - closeBtn.Width - 8, 12)
        End If

        ' Spawn initial particles AFTER form is shown (ClientRectangle now has correct values)
        Dim centerX As Single = CSng(Me.ClientRectangle.Width / 2)
        Dim centerY As Single = CSng(Me.ClientRectangle.Height / 2)
        For i As Integer = 0 To 50
            Dim angle As Double = Random.Shared.NextDouble() * Math.PI * 2
            Dim radius As Single = CSng(Random.Shared.NextDouble() * 200 + 30)
            particles.Add(New Particle(
                centerX + CSng(Math.Cos(angle) * radius),
                centerY + CSng(Math.Sin(angle) * radius),
                Color.FromArgb(0, 229, 255)))
        Next

        ' Force initial paint to display particles immediately
        bgPanel?.Invalidate()
    End Sub

End Class

''' <summary>
''' Represents a small animated particle used for visual effects.
''' </summary>
Public NotInheritable Class Particle
    Public Property X As Single
    Public Property Y As Single
    Public Property VX As Single
    Public Property VY As Single
    Public Property Size As Single
    Public Property Alpha As Byte
    Public Property Color As Color

    Public Sub New(x As Single, y As Single, color As Color)
        Me.X = x
        Me.Y = y
        Me.Color = color
        ' Random velocity between -1 and 1
        Me.VX = CSng(Random.Shared.NextDouble() * 2 - 1)
        Me.VY = CSng(Random.Shared.NextDouble() * 2 - 1)
        ' Random size between 1 and 4 pixels
        Me.Size = CSng(Random.Shared.NextDouble() * 3 + 1)
        ' Start fully opaque
        Me.Alpha = 255
    End Sub
End Class

''' <summary>
''' Holds the definition of one menu item (used by MenuForm).
''' </summary>
Public NotInheritable Class MenuItemDef
    Public Property Name As String = String.Empty
    Public Property Title As String = String.Empty
    Public Property Description As String = String.Empty
    Public Property TargetType As Type = Nothing
    Public Property IconChar As Char = ChrW(0)
    Public Property GradientStart As Color = Color.Empty
    Public Property GradientEnd As Color = Color.Empty
End Class