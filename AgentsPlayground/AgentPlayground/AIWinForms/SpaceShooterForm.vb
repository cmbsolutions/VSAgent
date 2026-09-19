Imports System.Drawing.Drawing2D
Imports System.Reflection

''' <summary>
''' Top-down space shooting game.
''' Player controls a starfighter, shoots down enemy X-Wings with blue beam bullets.
''' Assets: stars.png (background), starfighter.png (player), XWing.png (enemies), beamBlue.png (bullets).
''' </summary>
Public Class SpaceShooterForm
    Inherits Form

    ' ===== Embedded resource image cache (loaded lazily in InitializeGame) =====
    Private imgStars As Image = Nothing
    Private imgPlayerShip As Image = Nothing
    Private imgEnemy As Image = Nothing
    Private imgBeamBlue As Image = Nothing

    ''' <summary>Loads an embedded resource image by file name. Returns Nothing if not found.</summary>
    Private Function LoadEmbeddedImage(fileName As String) As Image
        Dim asm As Assembly = Assembly.GetExecutingAssembly()
        Dim names() As String = asm.GetManifestResourceNames()
        For Each res In names
            If res.EndsWith(fileName, StringComparison.OrdinalIgnoreCase) Then
                Using stream As IO.Stream = asm.GetManifestResourceStream(res)
                    If stream IsNot Nothing Then
                        Return New Bitmap(stream)
                    End If
                End Using
            End If
        Next
        Return Nothing   ' Image not found as embedded resource
    End Function

    ''' <summary>Checks whether an image is a valid non-placeholder bitmap.</summary>
    Private Shared Function IsValidImage(img As Image) As Boolean
        Return img IsNot Nothing AndAlso img.Width >= 16 AndAlso img.Height >= 16
    End Function

    ' ===== Player state =====
    Private playerX As Double = 0
    Private playerY As Double = 0
    Private Const PLAYER_WIDTH As Integer = 40
    Private Const PLAYER_HEIGHT As Integer = 50
    Private playerAlive As Boolean = True

    ' ===== Enemies =====
    Private enemies As New List(Of EnemyShip)()
    Private enemySpawnTimer As Integer = 0
    Private enemySpawnInterval As Integer = 90
    Private enemySpeedBase As Double = 2.0
    Private enemyWaveCount As Integer = 0

    ' ===== Bullets (player beams) =====
    Private bullets As New List(Of Bullet)()
    Private bulletCooldown As Integer = 0
    Private Const BULLET_COOLDOWN As Integer = 8

    ' ===== Background scrolling =====
    Private scrollOffsetX As Double = 0.0R
    Private scrollOffsetY As Double = 0.0R

    ' ===== Score & lives =====
    Private score As Integer = 0
    Private lives As Integer = 3

    ' ===== Game state =====
    Private gameOver As Boolean = False

    ' ===== Keys held down =====
    Private keysDown As New HashSet(Of Keys)()

    ' ===== Explosion particles =====
    Private explosions As New List(Of ExplosionParticle)()

    ' ===== Respawn countdown =====
    Private respawnTimer As Integer = 0
    Private Const RESPAWN_FRAMES As Integer = 45

    ' ===== Screen shake =====
    Private shakeOffsetX As Double = 0.0R
    Private shakeOffsetY As Double = 0.0R
    Private shakeDuration As Integer = 0
    Private shakeIntensity As Double = 5.0

    ' ===== Random number generator =====
    Private rng As New Random()

    ' ===== Background scroll speed =====
    Private scrollSpeedX As Double = 0.5R
    Private scrollSpeedY As Double = 0.3R

    ' ===== Constants for collision hitbox (slightly smaller than visual) =====
    Private Const HITBOX_OFFSET As Integer = 4
    Private Const HITBOX_WIDTH As Integer = PLAYER_WIDTH - HITBOX_OFFSET * 2   ' = 32
    Private Const HITBOX_HEIGHT As Integer = PLAYER_HEIGHT - HITBOX_OFFSET * 2  ' = 42

    Public Sub New()
        InitializeComponent()
    End Sub

    Protected Overrides Sub OnShown(e As EventArgs)
        MyBase.OnShown(e)
        ' Load images from embedded resources if available, otherwise use fallback shapes
        imgStars = LoadEmbeddedImage("stars.png")
        imgPlayerShip = LoadEmbeddedImage("starfighter.png")
        imgEnemy = LoadEmbeddedImage("XWing.png")
        imgBeamBlue = LoadEmbeddedImage("beamBlue.png")

        InitializeGame()
    End Sub

    Private Sub InitializeGame()
        playerX = CDbl(gamePanel.Width - PLAYER_WIDTH) / 2.0R   ' center horizontally
        playerY = gamePanel.Height - PLAYER_HEIGHT - 40          ' near bottom
        playerAlive = True

        enemies.Clear()
        bullets.Clear()
        explosions.Clear()

        enemySpawnTimer = 0
        enemyWaveCount = 0
        enemySpeedBase = 2.0
        enemySpawnInterval = 90

        score = 0
        lives = 3
        gameOver = False

        scrollOffsetX = 0.0R
        scrollOffsetY = 0.0R

        respawnTimer = 0
        shakeDuration = 0

        gameTimer.Interval = 16   ' ~60 FPS
        gameTimer.Start()

        scoreLabel.Text = $"Score: {score}  |  Lives: {lives}"
        statusText.Text = "Arrow Keys/WASD to Move — Space to Shoot"
        restartButton.Visible = False
        gamePanel.Invalidate()
    End Sub

    ' ===== Game loop =====
    Private Sub gameTimer_Tick(sender As Object, e As EventArgs) Handles gameTimer.Tick
        UpdateGame()
        gamePanel.Invalidate()
    End Sub

    ''' <summary>Update all game entities for one frame.</summary>
    Private Sub UpdateGame()
        ' --- Background scroll ---
        scrollOffsetX += scrollSpeedX
        scrollOffsetY += scrollSpeedY
        If scrollOffsetX >= imgStars.Width Then scrollOffsetX -= imgStars.Width
        If scrollOffsetY >= imgStars.Height Then scrollOffsetY -= imgStars.Height

        ' --- Respawn handling ---
        If Not playerAlive Then
            respawnTimer -= 1
            Dim secondsLeft As Integer = CInt(Math.Max(0, respawnTimer) / 60 + 1)
            statusText.Text = $"Respawning in {secondsLeft}..."
            If respawnTimer <= 0 Then
                playerAlive = True
                playerX = CDbl(gamePanel.Width - imgPlayerShip.Width) / 2.0R
                playerY = gamePanel.Height - imgPlayerShip.Height - 40
                statusText.Text = "Arrow Keys/WASD to Move — Space to Shoot"
            End If
        Else
            ' --- Player movement ---
            Dim dx As Integer = 0
            Dim dy As Integer = 0
            If keysDown.Contains(Keys.Up) OrElse keysDown.Contains(Keys.W) Then dy -= 1
            If keysDown.Contains(Keys.Down) OrElse keysDown.Contains(Keys.S) Then dy += 1
            If keysDown.Contains(Keys.Left) OrElse keysDown.Contains(Keys.A) Then dx -= 1
            If keysDown.Contains(Keys.Right) OrElse keysDown.Contains(Keys.D) Then dx += 1

            ' Normalize diagonal movement
            If dx <> 0 AndAlso dy <> 0 Then
                dx = CInt(dx * 0.75R)
                dy = CInt(dy * 0.75R)
            End If

            playerX = Math.Max(0, Math.Min(gamePanel.Width - PLAYER_WIDTH, playerX + dx * 6))
            playerY = Math.Max(0, Math.Min(gamePanel.Height - PLAYER_HEIGHT, playerY + dy * 6))

            ' --- Shooting ---
            If bulletCooldown > 0 Then
                bulletCooldown -= 1
            End If
            If keysDown.Contains(Keys.Space) AndAlso bulletCooldown <= 0 Then
                FireBullet()
                bulletCooldown = BULLET_COOLDOWN
            End If
        End If

        ' --- Spawn enemies ---
        enemySpawnTimer += 1
        If enemySpawnTimer >= enemySpawnInterval Then
            enemySpawnTimer = 0
            SpawnEnemy()
            enemyWaveCount += 1
            ' Increase difficulty over time: spawn faster and slightly faster enemies
            If enemyWaveCount Mod 50 = 0 AndAlso enemySpawnInterval > 25 Then
                enemySpawnInterval -= 8
            End If
            If enemyWaveCount Mod 30 = 0 Then
                enemySpeedBase = Math.Min(enemySpeedBase + 0.3, 7.0)
            End If
        End If

        ' --- Update bullets ---
        For i As Integer = bullets.Count - 1 To 0 Step -1
            Dim by As Integer = bullets(i).y - CInt(bullets(i).speed)   ' move upward
            If by + bullets(i).height < 0 Then
                bullets.RemoveAt(i)   ' off screen top
            Else
                bullets(i) = New Bullet(bullets(i).x, by, bullets(i).width, bullets(i).height, bullets(i).speed)
            End If
        Next

        ' --- Update enemies ---
        For i As Integer = enemies.Count - 1 To 0 Step -1
            Dim e = enemies(i)
            e.Y += enemySpeedBase * e.speedMultiplier
            e.X += Math.Sin(e.wobble + e.wobblePhase) * 0.5   ' slight horizontal drift
            e.wobble += 0.03

            ' Remove if off screen bottom (with some margin so they fully exit)
            If e.Y > gamePanel.Height + 50 Then
                enemies.RemoveAt(i)
            End If
        Next

        ' --- Collision: bullets vs enemies ---
        Dim bulletsToRemove As New List(Of Integer)()
        Dim enemiesToRemove As New List(Of Integer)()

        For bi As Integer = bullets.Count - 1 To 0 Step -1
            Dim b = bullets(bi)
            For ei As Integer = enemies.Count - 1 To 0 Step -1
                Dim e = enemies(ei)
                If CheckCollision(b.x, b.y, b.width, b.height, e.X, e.Y, e.Width, e.Height) Then
                    bulletsToRemove.Add(bi)
                    enemiesToRemove.Add(ei)
                    ' Score
                    score += CInt(10 * e.pointsMultiplier)
                    ' Explosion particles at enemy center
                    For pi As Integer = 0 To 14 - 1
                        explosions.Add(New ExplosionParticle(e.X + e.Width / 2.0R, e.Y + e.Height / 2.0R, rng))
                    Next
                    Exit For
                End If
            Next
        Next

        ' Remove dead entities (iterate in reverse to avoid index issues)
        For Each idx In enemiesToRemove.OrderByDescending(Function(x) x)
            enemies.RemoveAt(idx)
        Next
        For Each idx In bulletsToRemove.OrderByDescending(Function(x) x)
            bullets.RemoveAt(idx)
        Next

        ' --- Collision: enemy vs player ---
        If playerAlive Then
            For ei As Integer = enemies.Count - 1 To 0 Step -1
                Dim e = enemies(ei)
                If CheckCollision(playerX + HITBOX_OFFSET, playerY + HITBOX_OFFSET, HITBOX_WIDTH, HITBOX_HEIGHT,
                                 e.X, e.Y, e.Width, e.Height) Then
                    playerAlive = False
                    lives -= 1
                    scoreLabel.Text = $"Score: {score}  |  Lives: {lives}"
                    explosions.Clear()   ' clear old particles before adding new explosion
                    For pi As Integer = 0 To 24 - 1
                        explosions.Add(New ExplosionParticle(playerX + PLAYER_WIDTH / 2.0, playerY + PLAYER_HEIGHT / 2.0, rng))
                    Next
                    enemies.RemoveAt(ei)
                    shakeDuration = CInt(shakeIntensity * 3)
                    shakeIntensity = 6.0

                    ' Check if player is out of lives
                    If lives <= 0 Then
                        gameOver = True
                        statusText.Text = "GAME OVER!"
                        restartButton.Visible = True
                    End If
                    Exit For
                End If
            Next
        End If

        ' --- Update explosion particles ---
        For i As Integer = explosions.Count - 1 To 0 Step -1
            explosions(i).life -= 1
            explosions(i).x += explosions(i).VX
            explosions(i).y += explosions(i).VY
            If explosions(i).life <= 0 Then
                explosions.RemoveAt(i)
            End If
        Next

        ' --- Screen shake decay ---
        If shakeDuration > 0 Then
            shakeDuration -= 1
            shakeOffsetX = (rng.NextDouble() * 2.0 - 1.0R) * shakeIntensity
            shakeOffsetY = (rng.NextDouble() * 2.0 - 1.0R) * shakeIntensity
            shakeIntensity *= 0.92R   ' decay
        Else
            shakeOffsetX = 0.0R
            shakeOffsetY = 0.0R
        End If
    End Sub

    Private Sub FireBullet()
        Dim bulletWidth As Integer = Math.Min(6, If(imgBeamBlue.Width > 0, CInt(Math.Max(4, imgBeamBlue.Width * 0.25)), 8))
        Dim bulletHeight As Integer = Math.Min(30, If(imgBeamBlue.Height > 0, CInt(Math.Max(10, imgBeamBlue.Height * 0.75)), 24))

        ' Create a couple of side-by-side beams for the player ship (like an X-wing style)
        Dim offset As Integer = CInt((imgPlayerShip.Width - bulletWidth) / 2)
        bullets.Add(New Bullet(CInt(playerX + offset), CInt(playerY), bulletWidth, bulletHeight, 10))   ' left beam
        bullets.Add(New Bullet(CInt(playerX + imgPlayerShip.Width - offset - bulletWidth), CInt(playerY), bulletWidth, bulletHeight, 10))  ' right beam
    End Sub

    Private Sub SpawnEnemy()
        Dim eW As Integer = If(imgEnemy.Width > 0, imgEnemy.Width, 40)
        Dim eH As Integer = If(imgEnemy.Height > 0, imgEnemy.Height, 40)
        Dim x As Double = rng.Next(0, CInt(gamePanel.Width - eW))
        Dim pointsMult As Single = CSng(rng.NextDouble() * 1.5 + 0.5)
        Dim speedMult As Single = CSng(rng.NextDouble() * 0.8 + 0.6)

        enemies.Add(New EnemyShip(x, -CDbl(eH), eW, eH, pointsMult, speedMult))
    End Sub

    ''' <summary>Axis-aligned bounding box collision check.</summary>
    Private Function CheckCollision(x1 As Double, y1 As Double, w1 As Double, h1 As Double,
                                    x2 As Double, y2 As Double, w2 As Double, h2 As Double) As Boolean
        Return x1 < x2 + w2 AndAlso x1 + w1 > x2 AndAlso y1 < y2 + h2 AndAlso y1 + h1 > y2
    End Function

    ' ===== Drawing =====
    Private Sub gamePanel_Paint(sender As Object, e As PaintEventArgs) Handles gamePanel.Paint
        Dim g As Graphics = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        g.InterpolationMode = InterpolationMode.HighQualityBilinear

        ' --- Clear ---
        g.Clear(Color.FromArgb(0, 0, 10))   ' deep space blue-black

        ' --- Scrolling starfield background (tiled) ---
        If imgStars.Width > 0 AndAlso imgStars.Height > 0 Then
            Dim tileW As Integer = imgStars.Width
            Dim tileH As Integer = imgStars.Height
            ' We need to tile enough times to cover the entire panel, accounting for scroll offsets
            For col As Integer = -1 To CInt(CSng(gamePanel.Width) / tileW) + 1
                For row As Integer = -1 To CInt(CSng(gamePanel.Height) / tileH) + 1
                    Dim drawX As Double = (col * tileW) - (scrollOffsetX Mod tileW)
                    Dim drawY As Double = (row * tileH) - (scrollOffsetY Mod tileH)
                    g.DrawImage(imgStars, CSng(drawX), CSng(drawY), CSng(tileW), CSng(tileH))
                Next
            Next
        Else
            g.Clear(Color.Black)
        End If

        ' --- Bullets (blue beams) ---
        For Each b In bullets
            If imgBeamBlue.Width > 0 AndAlso imgBeamBlue.Height > 0 Then
                g.DrawImage(imgBeamBlue, CSng(b.x), CSng(b.y), CSng(b.width), CSng(b.height))
            Else
                Using br As New SolidBrush(Color.FromArgb(80, 120, 255))
                    g.FillRectangle(br, CSng(b.x), CSng(b.y), CSng(b.width), CSng(b.height))
                End Using
            End If
        Next

        ' --- Enemies ---
        For Each ene In enemies
            If imgEnemy.Width > 0 AndAlso imgEnemy.Height > 0 Then
                g.DrawImage(imgEnemy, CSng(ene.X), CSng(ene.Y), CSng(ene.Width), CSng(ene.Height))
            End If
        Next

        ' --- Player ship ---
        If playerAlive Then
            If imgPlayerShip.Width > 0 AndAlso imgPlayerShip.Height > 0 Then
                g.DrawImage(imgPlayerShip, CSng(playerX), CSng(playerY), CSng(imgPlayerShip.Width), CSng(imgPlayerShip.Height))

                ' Engine flame effect (small yellow-orange flicker under the ship)
                Dim flameH As Integer = CInt(6 + rng.NextDouble() * 8)
                Using flameBrush As New SolidBrush(Color.FromArgb(CByte(150 + rng.Next(105)), 255, CByte(Math.Max(0, 100 + rng.Next(155))), 0))
                    g.FillRectangle(flameBrush, CSng(playerX + imgPlayerShip.Width * 0.3F), CSng(playerY + imgPlayerShip.Height), CSng(imgPlayerShip.Width * 0.4F), flameH)
                End Using
            Else
                ' Fallback: draw a simple triangle shape
                Using pen As New Pen(Color.FromArgb(0, 200, 255), 2.0F)
                    Dim pts() As PointF = {
                        New PointF(CSng(playerX + imgPlayerShip.Width / 2), CSng(playerY)),
                        New PointF(CSng(playerX), CSng(playerY + imgPlayerShip.Height)),
                        New PointF(CSng(playerX + imgPlayerShip.Width), CSng(playerY + imgPlayerShip.Height))
                    }
                    g.FillPolygon(Brushes.BlueViolet, pts)
                    g.DrawPolygon(pen, pts)
                End Using
            End If
        End If

        ' --- Draw explosion particles (unified, applies to both player death and enemy kills) ---
        For Each p In explosions
            Dim progress As Double = 1.0R - CSng(p.life) / 24.0R   ' 0 = fresh, 1 = dead
            Dim alpha As Byte = CByte(Math.Max(0, Math.Min(255, p.life * 12)))
            If alpha <= 0 Then Continue For

            Dim size As Single = CSng(progress * 30 + 2)
            ' Outer glow (red-orange)
            Using br As New SolidBrush(Color.FromArgb(alpha, 255, CByte(Math.Max(0, 150 - p.life * 6)), 0))
                g.FillEllipse(br, CSng(p.x - size), CSng(p.y - size), size * 2, size * 2)
            End Using
            ' Inner bright core (yellow-white)
            Dim innerSize As Single = size * 0.45R
            If innerSize > 1 Then
                Using br As New SolidBrush(Color.FromArgb(CByte(Math.Max(0, alpha - 30)), 255, 255, CByte(Math.Min(255, 200 + p.life * 4))))
                    g.FillEllipse(br, CSng(p.x - innerSize), CSng(p.y - innerSize), innerSize * 2, innerSize * 2)
                End Using
            End If
        Next

        ' --- HUD overlay: wave info at bottom of panel ---
        Dim hudFont As New Font("Consolas", 9.0F)
        Using hBrush As New SolidBrush(Color.FromArgb(100, Color.White))
            g.DrawString($"Wave: {enemyWaveCount}", hudFont, hBrush, 8, CSng(gamePanel.Height - 20))
        End Using

        ' --- Game Over overlay ---
        If gameOver Then
            Dim goFont As New Font("Segoe UI", 36.0F, FontStyle.Bold)
            Using br As New SolidBrush(Color.FromArgb(180, Color.Red))
                g.DrawString("GAME OVER", goFont, br, CSng((gamePanel.Width - CInt(g.MeasureString("GAME OVER", goFont).Width)) / 2), CSng(gamePanel.Height / 2 - 30))
            End Using
            Dim subFont As New Font("Segoe UI", 14.0F)
            Using br As New SolidBrush(Color.FromArgb(150, Color.White))
                g.DrawString($"Final Score: {score}", subFont, br, CSng((gamePanel.Width - CInt(g.MeasureString($"Final Score: {score}", subFont).Width)) / 2), CSng(gamePanel.Height / 2 + 20))
            End Using
            goFont.Dispose()
            subFont.Dispose()
        End If

        hudFont.Dispose()
    End Sub

    ' ===== Keyboard input =====
    Protected Overrides Sub OnKeyDown(e As KeyEventArgs)
        MyBase.OnKeyDown(e)
        keysDown.Add(e.KeyCode)
        e.SuppressKeyPress = True   ' prevent default system behavior
    End Sub

    Protected Overrides Sub OnKeyUp(e As KeyEventArgs)
        MyBase.OnKeyUp(e)
        keysDown.Remove(e.KeyCode)
        e.SuppressKeyPress = True
    End Sub

    Protected Overrides Sub OnFormClosed(e As FormClosedEventArgs)
        MyBase.OnFormClosed(e)
        gameTimer.Stop()
    End Sub

    Private Sub restartButton_Click(sender As Object, e As EventArgs) Handles restartButton.Click
        InitializeGame()
    End Sub

    ' ===== Data classes =====

    Private Class EnemyShip
        Public Property X As Double
        Public Property Y As Double
        Public ReadOnly Property Width As Integer
        Public ReadOnly Property Height As Integer
        Public Property speedMultiplier As Single
        Public Property pointsMultiplier As Single
        Public Property wobble As Double = 0.0
        Public Property wobblePhase As Double

        Public Sub New(x As Double, y As Double, width As Integer, height As Integer, pointsMult As Single, speedMult As Single)
            Me.X = x
            Me.Y = y
            Me.Width = width
            Me.Height = height
            Me.pointsMultiplier = pointsMult
            Me.speedMultiplier = speedMult
        End Sub
    End Class

    Private Structure Bullet
        Public ReadOnly Property x As Integer
        Public ReadOnly Property y As Integer
        Public ReadOnly Property width As Integer
        Public ReadOnly Property height As Integer
        Public ReadOnly Property speed As Integer

        Public Sub New(x As Integer, y As Integer, width As Integer, height As Integer, speed As Integer)
            Me.x = x
            Me.y = y
            Me.width = width
            Me.height = height
            Me.speed = speed
        End Sub
    End Structure

    Private Class ExplosionParticle
        Public Property x As Double
        Public Property y As Double
        Public Property VX As Single
        Public Property VY As Single
        Public Property life As Integer

        Public Sub New(originX As Double, originY As Double, randomizer As Random)
            Me.x = originX
            Me.y = originY
            Dim angle As Double = randomizer.NextDouble() * Math.PI * 2
            Dim speed As Single = CSng(randomizer.NextDouble() * 3 + 1)
            Me.VX = CSng(Math.Cos(angle) * speed)
            Me.VY = CSng(Math.Sin(angle) * speed)
            Me.life = randomizer.Next(10, 24)
        End Sub
    End Class

End Class