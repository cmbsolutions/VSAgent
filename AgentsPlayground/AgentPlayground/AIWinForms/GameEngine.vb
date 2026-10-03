Imports System.Collections.Generic
Imports System.Drawing

''' <summary>
''' Core game engine: owns all game state, runs the update loop with delta timing,
''' and provides a renderable entity list. The form is just a canvas for rendering.
''' </summary>
Public Class GameEngine
    Implements IDisposable

    ' ===== Timing =====
    Private ReadOnly tickInterval As TimeSpan = TimeSpan.FromMilliseconds(16) ' ~60 TPS target
    Private nextTickTime As Double = 0
    Private isRunning As Boolean = False
    Private frameCount As Integer = 0

    ' ===== Rendering surface =====
    Public Property SurfaceWidth As Integer = 900
    Public Property SurfaceHeight As Integer = 700

    ' ===== Sprites (set by the form from embedded resources) =====
    ''' <summary>Tiled starfield background image.</summary>
    Public Property StarsImage As Image = Nothing
    ''' <summary>Player ship sprite.</summary>
    Public Property PlayerImage As Image = Nothing
    ''' <summary>Enemy X-Wing sprite.</summary>
    Public Property EnemyImage As Image = Nothing
    ''' <summary>Bullet / blue beam sprite.</summary>
    Public Property BulletImage As Image = Nothing

    ' ===== Starfield scroll (used by renderer) =====
    Public Property PlayerX As Double = 430
    Public Property PlayerY As Double = 620
    Public Const PLAYER_WIDTH As Integer = 40
    Public Const PLAYER_HEIGHT As Integer = 50
    Public Property PlayerAlive As Boolean = True
    Private playerVX As Double = 0
    Private playerVY As Double = 0
    ' Acceleration and friction for responsive yet smooth movement
    Private Const PLAYER_ACCEL As Double = 0.8
    Private Const PLAYER_FRICTION As Double = 0.85
    Private Const PLAYER_MAX_SPEED As Double = 10

    ' ===== Enemies =====
    Public ReadOnly Property Enemies As New List(Of EnemyShip)()
    Private enemySpawnTimer As Double = 0
    Private enemySpawnInterval As Double = 90 / 60.0 ' frames to seconds: spawn every ~1.5s at 60fps
    Private enemySpeedBase As Double = 2.0
    Public EnemyWaveCount As Integer = 0

    ' ===== Bullets =====
    Public Property Bullets As New List(Of Bullet)()
    Private bulletCooldown As Double = 0
    Private Const BULLET_COOLDOWN As Double = 8 / 60.0 ' seconds

    ' ===== Starfield =====
    Public ReadOnly Property Stars As New List(Of StarFieldStar)()
    Public Property ScrollSpeedY As Double = 60.0 ' pixels per second — very fast scroll for speed sensation!
    Public ReadOnly Property ScrollOffsetYValue As Double
        Get
            Return scrollOffsetY
        End Get
    End Property
    Private scrollOffsetY As Double = 0

    ' ===== Explosions =====
    Public ReadOnly Property Explosions As New List(Of ExplosionParticle)()

    ' ===== Score, lives, game state =====
    Public Property Score As Integer = 0
    Public Property Lives As Integer = 3
    Public Property GameOver As Boolean = False
    Public Property StatusText As String = ""

    ' ===== Respawn =====
    Public Property RespawnTimer As Double = 0
    Private Const RESPAWN_FRAMES As Double = 45 / 60.0

    ' ===== Screen shake =====
    Public Property ShakeOffsetX As Double = 0.0
    Public Property ShakeOffsetY As Double = 0.0
    Private shakeDuration As Double = 0
    Private shakeIntensity As Double = 5.0

    ' ===== Input tracking =====
    Private keysDown As New HashSet(Of Keys)()

    ' ===== Random =====
    Private rng As New Random()

    ' ===== Game event callbacks (assigned by the form) =====
    Public Property OnGameOver As Action(Of Integer) = Nothing

    ''' <summary>Initialize a fresh game state.</summary>
    Public Sub New()
        InitializeStarfield()
        PlayerX = (SurfaceWidth - PLAYER_WIDTH) / 2.0
        PlayerY = SurfaceHeight - PLAYER_HEIGHT - 40
        StatusText = "Arrow Keys/WASD to Move — Space to Shoot"
    End Sub

    ''' <summary>Start or restart the game loop.</summary>
    Public Sub StartGame()
        ResetGame()
        isRunning = True
        nextTickTime = Environment.TickCount64 / 1000.0
    End Sub

    ''' <summary>Stop the game loop.</summary>
    Public Sub StopGame()
        isRunning = False
    End Sub

    ''' <summary>Reset all game state to initial values.</summary>
    Private Sub ResetGame()
        PlayerX = (SurfaceWidth - PLAYER_WIDTH) / 2.0
        PlayerY = SurfaceHeight - PLAYER_HEIGHT - 40
        PlayerAlive = True
        playerVX = 0
        playerVY = 0
        Lives = 3
        Score = 0
        GameOver = False
        Enemies.Clear()
        Bullets.Clear()
        Explosions.Clear()
        enemySpawnTimer = 0
        enemyWaveCount = 0
        enemySpeedBase = 2.0
        enemySpawnInterval = 90 / 60.0
        bulletCooldown = 0
        respawnTimer = 0
        shakeDuration = 0
        scrollOffsetY = 0
        StatusText = "Arrow Keys/WASD to Move — Space to Shoot"
        ' Do NOT clear sprite images — they are set by the form once and persist
    End Sub

    ''' <summary>Initialize the starfield with random stars spread across a large area.</summary>
    Private Sub InitializeStarfield()
        Stars.Clear()
        ' Create a grid of stars covering a larger scrollable area
        Const STAR_COUNT_X As Integer = 60
        Const STAR_COUNT_Y As Integer = 80
        Const SPREAD_W As Double = 2000.0
        Const SPREAD_H As Double = 3000.0
        For i As Integer = 0 To STAR_COUNT_X - 1
            For j As Integer = 0 To STAR_COUNT_Y - 1
                Stars.Add(New StarFieldStar(
                    rng.NextDouble() * SPREAD_W,
                    rng.NextDouble() * SPREAD_H,
                    rng.Next(1, 3))) ' star size 1-2 pixels
            Next
        Next
    End Sub

    ''' <summary>Register a key state change.</summary>
    Public Sub KeyDown(key As Keys)
        keysDown.Add(key)
    End Sub

    ''' <summary>Unregister a key state change.</summary>
    Public Sub KeyUp(key As Keys)
        keysDown.Remove(key)
    End Sub

    ''' <summary>One tick of the game loop (called ~60 times per second).</summary>
    Public Sub Update(deltaSeconds As Double)
        If Not PlayerAlive Then
            RespawnTimer -= deltaSeconds
            If RespawnTimer <= 0 Then
                PlayerAlive = True
                PlayerX = (SurfaceWidth - PLAYER_WIDTH) / 2.0
                PlayerY = SurfaceHeight - PLAYER_HEIGHT - 40
                StatusText = "Arrow Keys/WASD to Move — Space to Shoot"
            Else
                Dim secondsLeft As Integer = CInt(Math.Ceiling(-RespawnTimer))
                StatusText = $"Respawning in {secondsLeft}..."
            End If
            UpdateScreenShake(deltaSeconds)
            UpdateExplosions(deltaSeconds)
            Return
        End If

        ' === Player movement with acceleration/deceleration ===
        Dim accelX As Double = 0
        Dim accelY As Double = 0
        If keysDown.Contains(Keys.Up) OrElse keysDown.Contains(Keys.W) Then accelY -= PLAYER_ACCEL
        If keysDown.Contains(Keys.Down) OrElse keysDown.Contains(Keys.S) Then accelY += PLAYER_ACCEL
        If keysDown.Contains(Keys.Left) OrElse keysDown.Contains(Keys.A) Then accelX -= PLAYER_ACCEL
        If keysDown.Contains(Keys.Right) OrElse keysDown.Contains(Keys.D) Then accelX += PLAYER_ACCEL

        ' Apply acceleration (no diagonal normalization — both axes get full speed)
        playerVX += accelX
        playerVY += accelY

        ' Apply friction when no key is pressed in that direction
        If Not (keysDown.Contains(Keys.Left) OrElse keysDown.Contains(Keys.A)) Then
            playerVX *= PLAYER_FRICTION
            If Math.Abs(playerVX) < 0.1 Then playerVX = 0
        End If
        If Not (keysDown.Contains(Keys.Right) OrElse keysDown.Contains(Keys.D)) Then
            playerVX *= PLAYER_FRICTION
            If Math.Abs(playerVX) < 0.1 Then playerVX = 0
        End If
        If Not (keysDown.Contains(Keys.Up) OrElse keysDown.Contains(Keys.W)) Then
            playerVY *= PLAYER_FRICTION
            If Math.Abs(playerVY) < 0.1 Then playerVY = 0
        End If
        If Not (keysDown.Contains(Keys.Down) OrElse keysDown.Contains(Keys.S)) Then
            playerVY *= PLAYER_FRICTION
            If Math.Abs(playerVY) < 0.1 Then playerVY = 0
        End If

        ' Clamp to max speed
        Dim currentSpeed As Double = Math.Sqrt(playerVX * playerVX + playerVY * playerVY)
        If currentSpeed > PLAYER_MAX_SPEED Then
            Dim scale As Double = PLAYER_MAX_SPEED / currentSpeed
            playerVX *= scale
            playerVY *= scale
        End If

        ' Apply velocity to position
        PlayerX += playerVX
        PlayerY += playerVY

        ' Clamp to bounds
        PlayerX = Math.Max(0, Math.Min(SurfaceWidth - PLAYER_WIDTH, PlayerX))
        PlayerY = Math.Max(0, Math.Min(SurfaceHeight - PLAYER_HEIGHT, PlayerY))

        ' === Shooting ===
        bulletCooldown -= deltaSeconds
        If keysDown.Contains(Keys.Space) AndAlso bulletCooldown <= 0 Then
            FireBullet()
            bulletCooldown = BULLET_COOLDOWN
        End If

        ' === Starfield scroll (FAST — gives sense of speed) ===
        scrollOffsetY += ScrollSpeedY * deltaSeconds
        If scrollOffsetY >= 3000.0 Then
            scrollOffsetY -= 3000.0
        End If

        ' === Spawn enemies ===
        enemySpawnTimer += deltaSeconds
        If enemySpawnTimer >= enemySpawnInterval Then
            enemySpawnTimer = 0
            SpawnEnemy()
            enemyWaveCount += 1
            If enemyWaveCount Mod 50 = 0 AndAlso enemySpawnInterval > 25 / 60.0 Then
                enemySpawnInterval -= 8 / 60.0
            End If
            If enemyWaveCount Mod 30 = 0 Then
                enemySpeedBase = Math.Min(enemySpeedBase + 0.3, 7.0)
            End If
        End If

        ' === Update bullets ===
        For i As Integer = Bullets.Count - 1 To 0 Step -1
            Dim b = Bullets(i)
            b.Y -= CSng(b.Speed * deltaSeconds * 60)
            If b.Y + b.Height < 0 Then
                Bullets.RemoveAt(i)
            End If
        Next

        ' === Update enemies ===
        For i As Integer = Enemies.Count - 1 To 0 Step -1
            Dim e = Enemies(i)
            e.Y += enemySpeedBase * e.speedMultiplier * deltaSeconds * 60
            e.X += Math.Sin(e.wobble + e.wobblePhase) * 0.5 * deltaSeconds * 60
            e.wobble += 0.03

            If e.Y > SurfaceHeight + 50 Then
                Enemies.RemoveAt(i)
            End If
        Next

        ' === Collision: bullets vs enemies ===
        Dim bulletsToRemove As New List(Of Integer)()
        Dim enemiesToRemove As New List(Of Integer)()

        For bi As Integer = Bullets.Count - 1 To 0 Step -1
            Dim b = Bullets(bi)
            For ei As Integer = Enemies.Count - 1 To 0 Step -1
                If enemiesToRemove.Contains(ei) Then Continue For
                Dim e = Enemies(ei)
                If AABB(b.X, b.Y, b.Width, b.Height, e.X, e.Y, e.Width, e.Height) Then
                    bulletsToRemove.Add(bi)
                    enemiesToRemove.Add(ei)
                    Score += CInt(10 * e.pointsMultiplier)
                    For pi As Integer = 0 To 14 - 1
                        Explosions.Add(New ExplosionParticle(e.X + e.Width / 2.0, e.Y + e.Height / 2.0, rng))
                    Next
                    Exit For
                End If
            Next
        Next

        For Each idx In enemiesToRemove.OrderByDescending(Function(x) x)
            Enemies.RemoveAt(idx)
        Next
        For Each idx In bulletsToRemove.OrderByDescending(Function(x) x)
            Bullets.RemoveAt(idx)
        Next

        ' === Collision: enemy vs player ===
        For ei As Integer = Enemies.Count - 1 To 0 Step -1
            Dim e = Enemies(ei)
            If AABB(PlayerX + 4, PlayerY + 4, PLAYER_WIDTH - 8, PLAYER_HEIGHT - 8, e.X, e.Y, e.Width, e.Height) Then
                PlayerAlive = False
                Lives -= 1
                Explosions.Clear()
                For pi As Integer = 0 To 24 - 1
                    Explosions.Add(New ExplosionParticle(PlayerX + PLAYER_WIDTH / 2.0, PlayerY + PLAYER_HEIGHT / 2.0, rng))
                Next
                Enemies.RemoveAt(ei)
                shakeDuration = shakeIntensity * 3
                shakeIntensity = 6.0

                If Lives <= 0 Then
                    GameOver = True
                    StatusText = "GAME OVER!"
                    OnGameOver?.Invoke(Score)
                End If
                Exit For
            End If
        Next

        UpdateExplosions(deltaSeconds)
        UpdateScreenShake(deltaSeconds)
    End Sub

    ''' <summary>Update explosion particles.</summary>
    Private Sub UpdateExplosions(delta As Double)
        For i As Integer = Explosions.Count - 1 To 0 Step -1
            Explosions(i).life -= delta
            Explosions(i).x += Explosions(i).VX * delta * 60
            Explosions(i).y += Explosions(i).VY * delta * 60
            If Explosions(i).life <= 0 Then
                Explosions.RemoveAt(i)
            End If
        Next
    End Sub

    ''' <summary>Update screen shake.</summary>
    Private Sub UpdateScreenShake(delta As Double)
        If shakeDuration > 0 Then
            shakeDuration -= delta
            ShakeOffsetX = (rng.NextDouble() * 2.0 - 1.0) * shakeIntensity
            ShakeOffsetY = (rng.NextDouble() * 2.0 - 1.0) * shakeIntensity
            shakeIntensity *= 0.92
        Else
            ShakeOffsetX = 0.0
            ShakeOffsetY = 0.0
        End If
    End Sub

    Private Sub FireBullet()
        Dim bulletWidth As Integer = If(BulletImage IsNot Nothing AndAlso BulletImage.Width > 0, CInt(Math.Max(4, BulletImage.Width * 0.25)), 6)
        Dim bulletHeight As Integer = If(BulletImage IsNot Nothing AndAlso BulletImage.Height > 0, CInt(Math.Max(10, BulletImage.Height * 0.75)), 30)
        Dim offset As Integer = CInt((PLAYER_WIDTH - bulletWidth) / 2)
        Bullets.Add(New Bullet(CInt(PlayerX + offset), CInt(PlayerY), bulletWidth, bulletHeight, 10))
        Bullets.Add(New Bullet(CInt(PlayerX + PLAYER_WIDTH - offset - bulletWidth), CInt(PlayerY), bulletWidth, bulletHeight, 10))
    End Sub

    Private Sub SpawnEnemy()
        Dim eW As Integer = If(EnemyImage IsNot Nothing AndAlso EnemyImage.Width > 0, EnemyImage.Width, 40)
        Dim eH As Integer = If(EnemyImage IsNot Nothing AndAlso EnemyImage.Height > 0, EnemyImage.Height, 40)
        Dim x As Double = rng.Next(0, SurfaceWidth - eW)
        Dim pointsMult As Single = CSng(rng.NextDouble() * 1.5 + 0.5)
        Dim speedMult As Single = CSng(rng.NextDouble() * 0.8 + 0.6)
        Enemies.Add(New EnemyShip(x, -CDbl(eH), eW, eH, pointsMult, speedMult))
    End Sub

    Private Function AABB(x1 As Double, y1 As Double, w1 As Double, h1 As Double, x2 As Double, y2 As Double, w2 As Double, h2 As Double) As Boolean
        Return x1 < x2 + w2 AndAlso x1 + w1 > x2 AndAlso y1 < y2 + h2 AndAlso y1 + h1 > y2
    End Function

    ''' <summary>Dispose resources.</summary>
    Public Sub Dispose() Implements IDisposable.Dispose
        Stars.Clear()
        Enemies.Clear()
        Bullets.Clear()
        Explosions.Clear()
    End Sub
End Class


''' <summary>A single star in the scrolling starfield.</summary>
Public Structure StarFieldStar
    Public ReadOnly Property X As Double
    Public ReadOnly Property Y As Double
    Public ReadOnly Property Size As Integer

    Public Sub New(x As Double, y As Double, size As Integer)
        Me.X = x
        Me.Y = y
        Me.Size = size
    End Sub
End Structure


''' <summary>An enemy X-Wing ship.</summary>
Public Class EnemyShip
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


''' <summary>A bullet (blue beam).</summary>
Public Class Bullet
    Public Property X As Integer
    Public Property Y As Integer
    Public Property Width As Integer
    Public Property Height As Integer
    Public Property Speed As Integer

    Public Sub New(x As Integer, y As Integer, width As Integer, height As Integer, speed As Integer)
        Me.X = x
        Me.Y = y
        Me.Width = width
        Me.Height = height
        Me.Speed = speed
    End Sub
End Class

''' <summary>A particle in an explosion.</summary>
Public Class ExplosionParticle
    Public Property X As Double
    Public Property Y As Double
    Public Property VX As Single
    Public Property VY As Single
    Public Property Life As Double

    Public Sub New(originX As Double, originY As Double, randomizer As Random)
        Me.X = originX
        Me.Y = originY
        Dim angle As Double = randomizer.NextDouble() * Math.PI * 2
        Dim speed As Single = CSng(randomizer.NextDouble() * 3 + 1)
        Me.VX = CSng(Math.Cos(angle) * speed)
        Me.VY = CSng(Math.Sin(angle) * speed)
        Me.Life = randomizer.Next(10, 24) / 60.0 ' in seconds
    End Sub
End Class
