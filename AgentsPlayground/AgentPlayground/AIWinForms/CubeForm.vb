Imports System.Drawing.Drawing2D

''' <summary>
''' Displays a rotating 3D wireframe cube on a dark background.
''' </summary>
Public Class CubeForm
    Inherits Form

    Private WithEvents paintTimer As New Timer() With {.Interval = 16, .Enabled = True}
    Private angleX As Single = 0F
    Private angleY As Single = 0F
    Private angleZ As Single = 0F
    Private closeLabel As Label = Nothing
    Private speedSlider As TrackBar = Nothing
    Private speedLabel As Label = Nothing
    Private lineThicknessSlider As TrackBar = Nothing
    Private thicknessLabel As Label = Nothing
    Private currentThickness As Single = 1.0F
    Private lastTick As DateTime = DateTime.Now

    Public Sub New()
        Me.SuspendLayout()
        InitializeComponent()
        Me.ResumeLayout(False)
        Refresh()
    End Sub

    Protected Overrides Sub OnFormClosed(e As FormClosedEventArgs)
        MyBase.OnFormClosed(e)
        paintTimer.Stop()
        paintTimer.Dispose()
    End Sub

    Private Sub InitializeComponent()
        Me.Text = "Rotating 3D Cube"
        Me.Size = New Size(600, 500)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.BackColor = Color.FromArgb(10, 14, 28)
        Me.FormBorderStyle = FormBorderStyle.Sizable

        ' Drawing panel
        Dim cubePanel As New Panel() With {
            .Name = "cubePanel",
            .Dock = DockStyle.Fill,
            .BackColor = Color.FromArgb(10, 14, 28)
        }
        AddHandler cubePanel.Paint, AddressOf CubePanel_Paint

        ' Title label
        Dim titleLabel As New Label() With {
            .Name = "cubeTitle",
            .Text = "3D Rotating Cube",
            .Font = New Font("Segoe UI", 14.0F, FontStyle.Bold),
            .AutoSize = False,
            .Size = New Size(300, 28),
            .Location = New Point((Me.ClientSize.Width - 300) \ 2, 12),
            .ForeColor = Color.FromArgb(0, 229, 255),
            .TextAlign = ContentAlignment.MiddleCenter,
            .BackColor = Color.Transparent
        }

        ' Close button
        closeLabel = New Label() With {
            .Name = "closeBtn",
            .Text = "X",
            .Font = New Font("Segoe UI", 16.0F, FontStyle.Bold),
            .Size = New Size(36, 36),
            .Location = New Point(Me.ClientSize.Width - 46, 8),
            .ForeColor = Color.FromArgb(180, 200, 220),
            .BackColor = Color.Transparent,
            .Cursor = Cursors.Hand,
            .TextAlign = ContentAlignment.MiddleCenter
        }
        AddHandler closeLabel.MouseEnter, Sub(s, e) DirectCast(s, Label).ForeColor = Color.FromArgb(255, 80, 80)
        AddHandler closeLabel.MouseLeave, Sub(s, e) DirectCast(s, Label).ForeColor = Color.FromArgb(180, 200, 220)
        AddHandler closeLabel.Click, Sub(s, e) Me.Close()

        Me.Controls.Add(cubePanel)
        Me.Controls.Add(titleLabel)
        Me.Controls.Add(closeLabel)

        Me.DoubleBuffered = True

        ' Bottom control panel (holds both sliders)
        Dim bottomPanel As New Panel() With {
            .Name = "bottomPanel",
            .Dock = DockStyle.Bottom,
            .Height = 90,
            .BackColor = Color.FromArgb(8, 11, 22)
        }
        Me.Controls.Add(bottomPanel)

        ' Speed slider label
        speedLabel = New Label() With {
            .Name = "speedLabel",
            .Text = "Rotation Speed: 1.0x",
            .Font = New Font("Segoe UI", 9.5F),
            .AutoSize = False,
            .Size = New Size(260, 24),
            .Location = New Point((bottomPanel.Width - 260) \ 2, 8),
            .ForeColor = Color.FromArgb(180, 200, 220),
            .TextAlign = ContentAlignment.MiddleCenter,
            .BackColor = Color.Transparent
        }
        bottomPanel.Controls.Add(speedLabel)

        ' Speed slider
        speedSlider = New TrackBar() With {
            .Name = "speedSlider",
            .Minimum = 1,
            .Maximum = 40,
            .Value = 10,
            .LargeChange = 5,
            .SmallChange = 1,
            .TickStyle = TickStyle.None,
            .Dock = DockStyle.Bottom,
            .Height = 36,
            .Location = New Point(0, bottomPanel.Height - 38),
            .Width = bottomPanel.Width
        }
        AddHandler speedSlider.Scroll, AddressOf SpeedSlider_Scroll
        bottomPanel.Controls.Add(speedSlider)

        ' Thickness slider label
        thicknessLabel = New Label() With {
            .Name = "thicknessLabel",
            .Text = "Line Thickness / Vertex Size: 1.0x",
            .Font = New Font("Segoe UI", 9.5F),
            .AutoSize = False,
            .Size = New Size(360, 24),
            .Location = New Point((bottomPanel.Width - 360) \ 2, bottomPanel.Height - 70),
            .ForeColor = Color.FromArgb(180, 200, 220),
            .TextAlign = ContentAlignment.MiddleCenter,
            .BackColor = Color.Transparent
        }
        bottomPanel.Controls.Add(thicknessLabel)

        ' Thickness slider
        lineThicknessSlider = New TrackBar() With {
            .Name = "lineThicknessSlider",
            .Minimum = 1,
            .Maximum = 50,
            .Value = 10,
            .LargeChange = 5,
            .SmallChange = 1,
            .TickStyle = TickStyle.None,
            .Dock = DockStyle.Bottom,
            .Height = 36,
            .Location = New Point(0, bottomPanel.Height - 72),
            .Width = bottomPanel.Width
        }
        AddHandler lineThicknessSlider.Scroll, AddressOf LineThicknessSlider_Scroll
        bottomPanel.Controls.Add(lineThicknessSlider)

        ' Recalculate control positions after adding docked bottom controls
        titleLabel.Location = New Point((Me.ClientSize.Width - 300) \ 2, 12)
        closeLabel.Location = New Point(Me.ClientSize.Width - 46, 8)
    End Sub

    Private Function Project3D(x As Single, y As Single, z As Single, panelW As Integer, panelH As Integer, camDist As Single) As Point
        Dim scale As Single = 200F / Math.Max(1, camDist + z)
        Dim px As Integer = CInt(panelW \ 2 + x * scale)
        Dim py As Integer = CInt(panelH \ 2 - y * scale)
        Return New Point(px, py)
    End Function

    ''' <summary>Convert HSL (hue 0-360, saturation 0-1, lightness 0-1) to RGB Color.</summary>
    Private Function HslToRgb(hue As Double, sat As Double, light As Double) As Color
        Dim c = (2.0 * sat) * Math.Min(light, 1 - light)
        Dim x = c * (1 - Math.Abs((hue / 60.0) Mod 2 - 1))
        Dim m = light - c / 2
        Dim r As Double, g As Double, b As Double

        Select Case hue
            Case < 60:      r = c : g = x : b = 0
            Case < 120:    r = x : g = c : b = 0
            Case < 180:    r = 0 : g = c : b = x
            Case < 240:    r = 0 : g = x : b = c
            Case < 300:    r = x : g = 0 : b = c
            Case Else:      r = c : g = 0 : b = x
        End Select

        Return Color.FromArgb(255, CByte(Math.Min(255, (r + m) * 255)), CByte(Math.Min(255, (g + m) * 255)), CByte(Math.Min(255, (b + m) * 255)))
    End Function

    Private Sub CubePanel_Paint(sender As Object, e As PaintEventArgs)
        Dim g = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        g.InterpolationMode = InterpolationMode.HighQualityBilinear

        Dim panelW As Integer = e.ClipRectangle.Width
        Dim panelH As Integer = e.ClipRectangle.Height

        If panelW <= 0 OrElse panelH <= 0 Then Return
        g.Clear(Color.FromArgb(10, 14, 28))

        ' Camera distance for perspective
        Dim camDist As Single = 5F
        Dim hw As Single = 1.5F

        ' Define cube vertices (half-width)
        Dim vx(7) As Single, vy(7) As Single, vz(7) As Single
        vx(0) = -hw : vy(0) = -hw : vz(0) = -hw
        vx(1) = hw  : vy(1) = -hw : vz(1) = -hw
        vx(2) = hw  : vy(2) = hw  : vz(2) = -hw
        vx(3) = -hw : vy(3) = hw  : vz(3) = -hw
        vx(4) = -hw : vy(4) = -hw : vz(4) = hw
        vx(5) = hw  : vy(5) = -hw : vz(5) = hw
        vx(6) = hw  : vy(6) = hw  : vz(6) = hw
        vx(7) = -hw : vy(7) = hw  : vz(7) = hw

        ' Rotate each vertex around X axis
        Dim csx As Single = CSng(Math.Cos(angleX))
        Dim snx As Single = CSng(Math.Sin(angleX))
        For i As Integer = 0 To 7
            Dim newX As Single = vx(i)
            Dim newY As Single = vy(i) * csx - vz(i) * snx
            Dim newZ As Single = vy(i) * snx + vz(i) * csx
            vy(i) = newY
            vz(i) = newZ
        Next

        ' Rotate each vertex around Y axis
        Dim csy As Single = CSng(Math.Cos(angleY))
        Dim sny As Single = CSng(Math.Sin(angleY))
        For i As Integer = 0 To 7
            Dim newX As Single = vx(i) * csy + vz(i) * sny
            Dim newZ As Single = -vx(i) * sny + vz(i) * csy
            vx(i) = newX
            vz(i) = newZ
        Next

        ' Rotate each vertex around Z axis
        Dim csz As Single = CSng(Math.Cos(angleZ))
        Dim snz As Single = CSng(Math.Sin(angleZ))
        For i As Integer = 0 To 7
            Dim newX As Single = vx(i) * csz - vy(i) * snz
            Dim newY As Single = vx(i) * snz + vy(i) * csz
            vx(i) = newX
            vy(i) = newY
        Next

        ' Define edges (pairs of vertex indices)
        Dim edgeData()() As Integer = {
            New Integer() {0, 1}, New Integer() {1, 2}, New Integer() {2, 3}, New Integer() {3, 0},
            New Integer() {4, 5}, New Integer() {5, 6}, New Integer() {6, 7}, New Integer() {7, 4},
            New Integer() {0, 4}, New Integer() {1, 5}, New Integer() {2, 6}, New Integer() {3, 7}
        }

        ' Draw edges with depth-based coloring and flowing hue
        Dim hueOffset As Double = (DateTime.Now - DateTime.Today).TotalSeconds * 60.0

        For Each edge In edgeData
            Dim i1 As Integer = edge(0)
            Dim i2 As Integer = edge(1)

            ' Average Z for depth cueing
            Dim avgZ As Single = (vz(i1) + vz(i2)) / 2.0F
            Dim alphaVal As Integer = CByte(Math.Max(50, Math.Min(255, CInt((1 - avgZ / camDist) * 200))))

            ' Depth-based brightness for saturation/lightness
            Dim depthFactor As Double = (1 - avgZ / camDist)
            Dim sat As Double = Math.Max(0.3, depthFactor)
            Dim light As Double = Math.Max(0.2, Math.Min(0.8, 0.5 + 0.3 * depthFactor))

            ' Flowing hue: base hue shifts over time, per-edge offset for variety
            Dim edgeHue As Double = ((hueOffset + avgZ * 30 + edge.Length * 120) Mod 360 + 360) Mod 360
            Dim edgeColor As Color = HslToRgb(edgeHue, sat, light)

            Using pen As New Pen(Color.FromArgb(alphaVal, edgeColor.R, edgeColor.G, edgeColor.B), 2.0F * currentThickness)
                Dim p1 As Point = Project3D(vx(i1), vy(i1), vz(i1), panelW, panelH, camDist)
                Dim p2 As Point = Project3D(vx(i2), vy(i2), vz(i2), panelW, panelH, camDist)
                g.DrawLine(pen, p1.X, p1.Y, p2.X, p2.Y)
            End Using
        Next

        ' Draw vertices as glowing dots with flowing colors
        For i As Integer = 0 To 7
            Dim depthFactorV As Double = (1 - vz(i) / camDist)
            Dim satV As Double = Math.Max(0.4, depthFactorV)
            Dim lightV As Double = Math.Max(0.3, Math.Min(0.8, 0.55 + 0.25 * depthFactorV))
            Dim vertexHue As Double = ((hueOffset + vz(i) * 40 + i * 45) Mod 360 + 360) Mod 360
            Dim vertexColor As Color = HslToRgb(vertexHue, satV, lightV)

            Using brush As New SolidBrush(Color.FromArgb(255, vertexColor.R, vertexColor.G, vertexColor.B))
                Dim p As Point = Project3D(vx(i), vy(i), vz(i), panelW, panelH, camDist)
                Dim dotSize As Single = 6F * currentThickness
                g.FillEllipse(brush, p.X - (dotSize \ 2), p.Y - (dotSize \ 2), dotSize, dotSize)
            End Using
        Next

        ' Subtle radial glow in center (using concentric circles to simulate gradient)
        Dim cx As Integer = panelW \ 2
        Dim cy As Integer = panelH \ 2
        For radius As Integer = 150 To 1 Step -3
            Dim alphaG As Byte = CByte(5 * (radius / 150))
            If alphaG > 0 Then
                Using brush As New SolidBrush(Color.FromArgb(alphaG, 0, 180, 255))
                    g.FillEllipse(brush, cx - radius, cy - radius, radius * 2, radius * 2)
                End Using
            End If
        Next
    End Sub

    Private Sub paintTimer_Tick(sender As Object, e As EventArgs) Handles paintTimer.Tick
        Dim elapsed As Double = (DateTime.Now - lastTick).TotalSeconds
        lastTick = DateTime.Now

        ' Rotation speed multiplier from slider (0.1x to 4.0x, default 1.0x)
        Dim speedMultiplier As Single = If(speedSlider IsNot Nothing, CSng(speedSlider.Value) / 10F, 1F)

        ' Scaled by elapsed time for smooth frame-rate independence
        angleX += CSng(elapsed * 3.5 * speedMultiplier)
        angleY += CSng(elapsed * 4.8 * speedMultiplier)
        angleZ += CSng(elapsed * 2.0 * speedMultiplier)

        Dim panel = TryCast(Me.Controls("cubePanel"), Panel)
        If panel IsNot Nothing Then
            panel.Invalidate()
        End If
    End Sub

    Private Sub SpeedSlider_Scroll(sender As Object, e As EventArgs)
        Dim value = DirectCast(sender, TrackBar).Value / 10.0F
        speedLabel.Text = $"Rotation Speed: {value:N1}x"
    End Sub

    Private Sub LineThicknessSlider_Scroll(sender As Object, e As EventArgs)
        Dim value = DirectCast(sender, TrackBar).Value / 10.0F
        thicknessLabel.Text = $"Line Thickness / Vertex Size: {value:N1}x"
        currentThickness = value

        ' Refresh the cube panel so changes are visible immediately
        Dim panel = TryCast(Me.Controls("cubePanel"), Panel)
        If panel IsNot Nothing Then
            panel.Invalidate()
        End If
    End Sub

    Protected Overrides Sub OnSizeChanged(e As EventArgs)
        MyBase.OnSizeChanged(e)
        If closeLabel IsNot Nothing Then
            closeLabel.Location = New Point(Me.ClientSize.Width - 46, 8)
        End If
    End Sub
End Class