Imports System.ComponentModel
Imports System.Drawing.Drawing2D
Imports System.Reflection
Imports System.Diagnostics

''' <summary>
''' Main menu form that allows the user to navigate to other application forms.
''' </summary>
Public Class MenuForm
    Inherits Form

    ' Animation timer and state
    Private WithEvents animTimer As New Timer() With {.Interval = 16, .Enabled = True} ' ~60 FPS
    Private tickCounter As Integer = 0
    Private particles As List(Of Particle) = New List(Of Particle)()
    Private gridOffset As Single = 0.0F
    Private selectedPanel As Panel = Nothing

    ' Hover animation state
    Private hoverTargets As Dictionary(Of Panel, (originX As Single, originY As Single, currentScale As Single)) = New Dictionary(Of Panel, (Single, Single, Single))()
    Private targetScales As Dictionary(Of Panel, Single) = New Dictionary(Of Panel, Single)()

    Public Sub New()
        InitializeComponent()

        ' Ensure layout is complete before showing
        Me.SuspendLayout()
        Me.ResumeLayout()
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
        Dim bgPanel As New Panel() With {
            .Name = "bgLayer",
            .Dock = DockStyle.Fill,
            .BackColor = Color.Transparent
        }
        AddHandler bgPanel.Paint, AddressOf GlassPanel_Paint
        Me.Controls.Add(bgPanel)

        ' ====== Top Accent Bar (with gradient) ======
        Dim topBar As New Panel() With {
            .Name = "topAccentBar",
            .Dock = DockStyle.Top,
            .Height = 3,
            .BackColor = Color.FromArgb(0, 229, 255)
        }
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

        ' ====== Create Menu Items with advanced styling ======
        Dim menuDefinitions As List(Of (Name As String, Title As String, Description As String, TargetType As Type, AccentColor As Color, IconChar As Char, GradientStart As Color, GradientEnd As Color)) = New List(Of (String, String, String, Type, Color, Char, Color, Color)) From {
            ("btn_OpenAIClientForm", "Neural Network Explorer", "Connect to OpenAI's advanced language models for AI-powered interactions", GetType(OpenAIClientForm), Color.FromArgb(0, 229, 255), "◉", Color.FromArgb(0, 180, 255), Color.FromArgb(147, 60, 255)),
            ("btn_JSONParser", "Data Structure Analyzer", "Visualize and parse JSON data with interactive hierarchical tree view", GetType(Form1), Color.FromArgb(32, 145, 210), "◈", Color.FromArgb(32, 145, 210), Color.FromArgb(69, 200, 255))
        }

        For i As Integer = 0 To menuDefinitions.Count - 1
            Dim def = menuDefinitions(i)
            Dim menuItemPanel As New Panel() With {
                .Name = def.Name,
                .Size = New Size(560, 70),
                .Location = New Point(20, i * 85),
                .BackColor = Color.FromArgb(18, 24, 38),
                .BorderStyle = BorderStyle.None,
                .Cursor = Cursors.Hand
            }

            ' Glowing border effect
            Dim glowBorder As New Panel() With {
                .Name = $"glow_{def.Name}",
                .Size = New Size(562, 72),
                .Location = New Point(-1, -1),
                .BackColor = Color.Transparent
            }
            AddHandler glowBorder.Paint, Sub(s, e)
                                             Dim g As Graphics = e.Graphics
                                             g.SmoothingMode = SmoothingMode.AntiAlias
                                             Using pen As New Pen(def.AccentColor, 2.0F)
                                                 pen.EndCap = LineCap.Round
                                                 pen.StartCap = LineCap.Round
                                                 g.DrawRectangle(pen, 0, 0, 561, 71)
                                             End Using
                                         End Sub
            menuItemPanel.Controls.Add(glowBorder)

            ' Accent gradient bar on the left
            Dim accentBar As New Panel() With {
                .Name = $"accent_{def.Name}",
                .Size = New Size(4, 50),
                .Location = New Point(12, 10),
                .BackColor = Color.Transparent
            }
            AddHandler accentBar.Paint, Sub(s, e)
                                            If e.ClipRectangle.Width <= 0 OrElse e.ClipRectangle.Height <= 0 Then Return
                                            Dim g As Graphics = e.Graphics
                                            Using brush As New LinearGradientBrush(e.ClipRectangle, def.GradientStart, def.GradientEnd, LinearGradientMode.Vertical)
                                                g.FillRectangle(brush, e.ClipRectangle)
                                            End Using
                                        End Sub
            menuItemPanel.Controls.Add(accentBar)

            ' Icon with gradient background
            Dim iconBg As New Panel() With {
                .Name = $"iconBg_{def.Name}",
                .Size = New Size(36, 36),
                .Location = New Point(28, 17),
                .BackColor = Color.FromArgb(10, 15, 28)
            }
            AddHandler iconBg.Paint, Sub(s, e)
                                         If e.ClipRectangle.Width <= 0 OrElse e.ClipRectangle.Height <= 0 Then Return
                                         Dim g As Graphics = e.Graphics
                                         g.SmoothingMode = SmoothingMode.AntiAlias
                                         Using brush As New LinearGradientBrush(e.ClipRectangle, def.GradientStart, def.GradientEnd, LinearGradientMode.BackwardDiagonal)
                                             g.FillEllipse(brush, 0, 0, 36, 36)
                                         End Using
                                     End Sub
            menuItemPanel.Controls.Add(iconBg)

            Dim iconLabel As New Label() With {
                .Name = $"icon_{def.Name}",
                .Text = def.IconChar.ToString(),
                .Font = New Font("Segoe UI Emoji", 16.0F),
                .Location = New Point(30, 19),
                .AutoSize = False,
                .Size = New Size(32, 32),
                .ForeColor = Color.White,
                .TextAlign = ContentAlignment.MiddleCenter,
                .BackColor = Color.Transparent
            }
            menuItemPanel.Controls.Add(iconLabel)

            ' Title
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
            menuItemPanel.Controls.Add(titleLabel)

            ' Description
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
            menuItemPanel.Controls.Add(descLabel)

            ' Arrow indicator
            Dim arrowPanel As New Panel() With {
                .Name = $"arrow_{def.Name}",
                .Size = New Size(40, 36),
                .Location = New Point(510, 17),
                .BackColor = Color.Transparent
            }
            AddHandler arrowPanel.Paint, Sub(s, e)
                                             Dim g As Graphics = e.Graphics
                                             g.SmoothingMode = SmoothingMode.AntiAlias
                                             Using pen As New Pen(def.AccentColor, 2.5F)
                                                 g.DrawLine(pen, 0, 18, 30, 18)
                                             End Using
                                             Using brush As New SolidBrush(def.AccentColor)
                                                 g.FillPolygon(brush, {New Point(24, 10), New Point(36, 18), New Point(24, 26)})
                                             End Using
                                         End Sub
            menuItemPanel.Controls.Add(arrowPanel)

            ' Hover and click handlers
            Dim hoverHandler = Sub(s As Object, e As EventArgs)
                                   DirectCast(s, Panel).BackColor = Color.FromArgb(25, 33, 50)
                               End Sub
            Dim leaveHandler = Sub(s As Object, e As EventArgs)
                                   DirectCast(s, Panel).BackColor = Color.FromArgb(18, 24, 38)
                               End Sub
            AddHandler menuItemPanel.MouseEnter, hoverHandler
            AddHandler menuItemPanel.MouseLeave, leaveHandler

            Dim clickHandler = Sub(s As Object, e As EventArgs)
                                   Dim ft As Type = def.TargetType
                                   If ft IsNot Nothing Then
                                       Dim instance As Form = CType(Activator.CreateInstance(ft), Form)
                                       instance.Show()
                                   End If
                               End Sub

            AddHandler menuItemPanel.Click, clickHandler
            AddHandler accentBar.Click, clickHandler
            AddHandler iconLabel.Click, clickHandler
            AddHandler titleLabel.Click, clickHandler
            AddHandler descLabel.Click, clickHandler
            AddHandler arrowPanel.Click, clickHandler

            ' Register for particle spawning on hover
            AddHandler menuItemPanel.MouseEnter, Sub(s As Object, e As EventArgs)
                                                     Dim panel = DirectCast(s, Panel)
                                                     For j As Integer = 0 To 3
                                                         particles.Add(New Particle(
                                                             panel.Left + panel.Width / 2,
                                                             panel.Top + panel.Height / 2,
                                                             def.AccentColor))
                                                     Next
                                                 End Sub

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

        ' Close button (top-right corner)
        Dim closeButton As New Label() With {
            .Name = "closeButton",
            .Text = "×",
            .Font = New Font("Segoe UI", 14.0F, FontStyle.Bold),
            .Size = New Size(36, 36),
            .Location = New Point(Me.Width - 36, 12),
            .ForeColor = Color.FromArgb(180, 200, 220),
            .BackColor = Color.Transparent,
            .Cursor = Cursors.Hand,
            .TextAlign = ContentAlignment.MiddleCenter
        }
        AddHandler closeButton.MouseEnter, Sub(s, e) DirectCast(s, Label).ForeColor = Color.FromArgb(255, 80, 80)
        AddHandler closeButton.MouseLeave, Sub(s, e) DirectCast(s, Label).ForeColor = Color.FromArgb(180, 200, 220)
        AddHandler closeButton.Click, Sub(s, e) Me.Close()
        Me.Controls.Add(closeButton)

        ' Status bar text
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

    ' ====== Glass Panel Background Effect ======
    Private Sub GlassPanel_Paint(sender As Object, e As PaintEventArgs)
        Dim g As Graphics = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias

        If Me.Width <= 0 OrElse Me.Height <= 0 Then Return

        ' Animated grid background
        g.Clear(Color.FromArgb(8, 12, 24))

        Dim gridSize As Integer = 40
        Dim offset As Single = CSng(tickCounter * 0.5) Mod gridSize

        Using pen As New Pen(Color.FromArgb(15, 30, 60), 1)
            For x As Integer = -gridSize + CInt(offset) To Me.Width + gridSize Step gridSize
                g.DrawLine(pen, x, 0, x, Me.Height)
            Next
            For y As Integer = -gridSize + CInt(offset) To Me.Height + gridSize Step gridSize
                g.DrawLine(pen, 0, y, Me.Width, y)
            Next
        End Using

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

        ' ====== Draw particles ======
        For Each particle In particles.ToList()
            If particle.Alpha > 0 Then
                Using brush As New SolidBrush(Color.FromArgb(particle.Alpha, particle.Color))
                    g.FillEllipse(brush, particle.X - particle.Size / 2, particle.Y - particle.Size / 2, particle.Size, particle.Size)
                End Using

                particle.X += CSng(particle.VX)
                particle.Y += CSng(particle.VY)
                particle.Alpha -= 5
            End If
        Next

        particles.RemoveAll(Function(p) p.Alpha <= 0)

        ' Add new ambient particles periodically
        tickCounter += 1
        If tickCounter Mod 30 = 0 AndAlso particles.Count < 50 Then
            Dim angle As Double = Random.Shared.NextDouble() * Math.PI * 2
            Dim radius As Single = CSng(Random.Shared.NextDouble() * 100)
            particles.Add(New Particle(
                centerX + CSng(Math.Cos(angle) * radius),
                centerY + CSng(Math.Sin(angle) * radius),
                Color.FromArgb(0, 229, 255)))
        End If
    End Sub

    ' ====== Title Panel Gradient Animation ======
    Private Sub TitlePanel_Paint(sender As Object, e As PaintEventArgs)
        Dim g As Graphics = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias

        If e.ClipRectangle.Width <= 0 OrElse e.ClipRectangle.Height <= 0 Then Return

        Using brush As New LinearGradientBrush(e.ClipRectangle, Color.FromArgb(10, 20, 40), Color.FromArgb(20, 35, 65), LinearGradientMode.Horizontal)
            g.FillRectangle(brush, e.ClipRectangle)
        End Using

        ' Animated top border glow
        Dim glowColor As Byte = CByte(100 + 155 * Math.Abs(Math.Sin(tickCounter * 0.02)))
        Using pen As New Pen(Color.FromArgb(glowColor, 0, 229, 255), 2)
            g.DrawLine(pen, 0, 0, CSng(sender.Width), 0)
        End Using
    End Sub

    ' ====== Top Bar Paint (Animated gradient color) ======
    Private Sub TopBar_Paint(sender As Object, e As PaintEventArgs)
        Dim g As Graphics = e.Graphics

        If e.ClipRectangle.Width <= 0 OrElse e.ClipRectangle.Height <= 0 Then Return

        Dim glowColor As Byte = CByte(127 + 128 * Math.Abs(Math.Sin(tickCounter * 0.03)))
        Using brush As New LinearGradientBrush(e.ClipRectangle,
                                              Color.FromArgb(glowColor, 0, 180, 255),
                                              Color.FromArgb(glowColor, 147, 60, 255),
                                              LinearGradientMode.Horizontal)
            g.FillRectangle(brush, e.ClipRectangle)
        End Using
    End Sub

End Class