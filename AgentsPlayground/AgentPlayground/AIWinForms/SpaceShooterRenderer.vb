Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Drawing.Imaging

''' <summary>
''' Renders all game entities onto a Graphics surface. Pure rendering — no game logic.
''' Perf rules followed here: nothing is allocated per frame (fonts, pens, brushes and
''' sprites are cached), antialiasing is off unless a vector shape needs it, and the
''' starfield is composited once into a strip that is scrolled with a single blit.
''' </summary>
Public Class SpaceShooterRenderer

    ' ===== Cached GDI+ resources: allocated once, reused for the whole session =====
    Private Shared ReadOnly HudFont As New Font("Consolas", 14.0F, FontStyle.Bold)
    Private Shared ReadOnly GameOverFont As New Font("Segoe UI", 36.0F, FontStyle.Bold)
    Private Shared ReadOnly GameOverSubFont As New Font("Segoe UI", 14.0F, FontStyle.Regular)
    Private Shared ReadOnly WhiteBrush As New SolidBrush(Color.White)
    Private Shared ReadOnly OverlayBrush As New SolidBrush(Color.FromArgb(100, 0, 0, 0))
    Private Shared ReadOnly GameOverBrush As New SolidBrush(Color.FromArgb(180, 255, 0, 0))
    Private Shared ReadOnly GameOverSubBrush As New SolidBrush(Color.FromArgb(150, 255, 255, 255))
    ' Mutable single-instance brushes: only .Color is changed per draw, never a new object.
    Private Shared ReadOnly ParticleOuterBrush As New SolidBrush(Color.Black)
    Private Shared ReadOnly ParticleInnerBrush As New SolidBrush(Color.Black)
    Private Shared ReadOnly FlameBrush As New SolidBrush(Color.FromArgb(255, 255, 170, 0))
    Private Shared ReadOnly BulletCoreBrush As New SolidBrush(Color.FromArgb(128, 140, 255))
    Private Shared ReadOnly BulletGlowBrush As New SolidBrush(Color.FromArgb(60, 100, 200, 255))
    Private Shared ReadOnly EnemyFillBrush As New SolidBrush(Color.DarkRed)
    Private Shared ReadOnly EnemyPen As New Pen(Color.Red, 2.0F)
    Private Shared ReadOnly PlayerFillBrush As New SolidBrush(Color.BlueViolet)
    Private Shared ReadOnly PlayerPen As New Pen(Color.FromArgb(0, 200, 255), 2.0F)

    ' ===== Cached, pre-composited starfield strip =====
    Private Shared starStrip As Bitmap
    Private Shared starStripKey As String
    ' Cached procedural star field (fallback path) so Random is not re-seeded per frame.
    Private Shared fallbackStars As FallbackStar()
    Private Shared fallbackStarsKey As String

    ' ===== Pre-scaled sprite cache (scaled once, then blitted unscaled) =====
    Private Shared ReadOnly spriteCache As New Dictionary(Of String, Bitmap)(StringComparer.Ordinal)

    ''' <summary>
    ''' Releases every cached bitmap. Call when sprites are (re)loaded, when the game
    ''' surface changes size, and when the game form closes.
    ''' </summary>
    Public Shared Sub ResetCaches()
        If starStrip IsNot Nothing Then
            starStrip.Dispose()
            starStrip = Nothing
            starStripKey = Nothing
        End If
        For Each b In spriteCache.Values
            b.Dispose()
        Next
        spriteCache.Clear()
        gameOverFinalWidths.Clear()
    End Sub

    ''' <summary>Draw the entire game frame.</summary>
    Public Shared Sub Render(g As Graphics, engine As GameEngine)
        ' Cheapest render state; AA is enabled locally only for vector shapes.
        g.SmoothingMode = SmoothingMode.None
        g.InterpolationMode = InterpolationMode.NearestNeighbor   ' pixel-perfect sprites
        g.PixelOffsetMode = PixelOffsetMode.Half
        g.CompositingMode = CompositingMode.SourceOver

        ' The cached star strip is opaque and covers the whole surface, so a separate
        ' clear is only needed on the procedural fallback path.
        EnsureStarStrip(engine)
        If starStrip Is Nothing Then g.Clear(Color.FromArgb(0, 0, 10))

        ' Apply screen shake offset when active
        If engine.ShakeOffsetX <> 0 OrElse engine.ShakeOffsetY <> 0 Then
            g.TranslateTransform(CSng(engine.ShakeOffsetX), CSng(engine.ShakeOffsetY))
        End If

        RenderStarfield(g, engine)
        RenderBullets(g, engine)
        RenderEnemies(g, engine)
        RenderPlayer(g, engine)
        RenderExplosions(g, engine)
        RenderHUD(g, engine)
        RenderGameOver(g, engine)

        ' Restore transform after shake
        If engine.ShakeOffsetX <> 0 OrElse engine.ShakeOffsetY <> 0 Then
            g.ResetTransform()
        End If
    End Sub

    ' ===== Starfield strip geometry: extra margin so screen-shake never exposes an edge =====
    Private Const STRIP_MARGIN As Integer = 16

    ''' <summary>
    ''' Builds (once) an opaque strip that already contains the tiled starfield and is
    ''' tall enough to absorb the scroll offset. Scrolling then costs one unscaled blit
    ''' instead of hundreds of alpha-blended stretch blits.
    ''' </summary>
    Private Shared Sub EnsureStarStrip(engine As GameEngine)
        Dim img As Image = engine.StarsImage
        If img Is Nothing OrElse img.Width < 16 OrElse img.Height < 16 Then
            If starStrip IsNot Nothing Then
                starStrip.Dispose()
                starStrip = Nothing
                starStripKey = Nothing
            End If
            Return
        End If

        Dim key = $"{img.Width}x{img.Height}_{engine.SurfaceWidth}x{engine.SurfaceHeight}"
        If starStrip IsNot Nothing AndAlso starStripKey = key Then Return

        If starStrip IsNot Nothing Then
            starStrip.Dispose()
            starStrip = Nothing
        End If

        Dim tileW As Integer = img.Width
        Dim tileH As Integer = img.Height
        Dim w As Integer = engine.SurfaceWidth + 2 * STRIP_MARGIN
        ' One extra tile row guarantees the strip always covers the surface at any offset.
        Dim h As Integer = engine.SurfaceHeight + 2 * STRIP_MARGIN + tileH

        ' Opaque 24bpp strip: blitting it is a plain copy (no per-pixel alpha blending),
        ' and it removes the need for a separate g.Clear() every frame.
        Dim strip As New Bitmap(w, h, PixelFormat.Format24bppRgb)
        Using gs As Graphics = Graphics.FromImage(strip)
            gs.Clear(Color.FromArgb(0, 0, 10))
            gs.InterpolationMode = InterpolationMode.NearestNeighbor
            gs.PixelOffsetMode = PixelOffsetMode.Half
            Dim y As Integer = -STRIP_MARGIN
            While y < h
                Dim x As Integer = -STRIP_MARGIN
                While x < w
                    gs.DrawImageUnscaled(img, x, y)
                    x += tileW
                End While
                y += tileH
            End While
        End Using

        starStrip = strip
        starStripKey = key
    End Sub

    ''' <summary>Draw the scrolling starfield.</summary>
    Private Shared Sub RenderStarfield(g As Graphics, engine As GameEngine)
        If starStrip IsNot Nothing Then
            Dim tileH As Integer = engine.StarsImage.Height
            Dim scroll As Integer = CInt(engine.ScrollOffsetYValue Mod tileH)
            ' Offsetting by the margin as well guarantees the strip still covers the
            ' surface when screen shake translates the whole frame in any direction.
            g.DrawImageUnscaled(starStrip, -STRIP_MARGIN, -scroll - STRIP_MARGIN)
            Return
        End If

        ' Fallback: procedural dots. Positions/alphas are generated once and cached —
        ' no Random, no brush allocations and no antialiasing in the per-frame path.
        Dim key = $"{engine.SurfaceWidth}x{engine.SurfaceHeight}"
        If fallbackStars Is Nothing OrElse fallbackStarsKey <> key Then
            Dim rng As New Random(42)
            ReDim fallbackStars(FALLBACK_STAR_COUNT - 1)
            For i As Integer = 0 To FALLBACK_STAR_COUNT - 1
                Dim s As New FallbackStar
                s.X = CSng(rng.NextDouble())          ' fraction of width
                s.Y = CSng(rng.NextDouble())          ' fraction of height
                s.Size = CSng(rng.Next(1, 3))
                s.Alpha = CByte(rng.Next(80, 200))
                fallbackStars(i) = s
            Next
            fallbackStarsKey = key
        End If

        Dim W As Single = engine.SurfaceWidth
        Dim H As Single = engine.SurfaceHeight
        Dim drift As Single = CSng(engine.ScrollOffsetYValue * 0.3)
        For i As Integer = 0 To FALLBACK_STAR_COUNT - 1
            Dim s As FallbackStar = fallbackStars(i)
            Dim sy As Single = (s.Y * H + drift) Mod H
            If sy < 0 Then sy += H
            Dim bucket As Integer = Math.Min(7, s.Alpha \ 32)
            If FallbackBrushes(bucket) Is Nothing Then
                FallbackBrushes(bucket) = New SolidBrush(Color.FromArgb(CByte((bucket + 1) * 32), Color.White))
            End If
            ' FillRectangle on 1-2 px dots is far cheaper than an antialiased ellipse.
            g.FillRectangle(FallbackBrushes(bucket), s.X * W, sy, s.Size, s.Size)
        Next
    End Sub

    Private Structure FallbackStar
        Public X As Single
        Public Y As Single
        Public Size As Single
        Public Alpha As Byte
    End Structure

    Private Const FALLBACK_STAR_COUNT As Integer = 150
    Private Shared ReadOnly FallbackBrushes(7) As SolidBrush

    ''' <summary>
    ''' Returns a pre-scaled copy of a sprite at the requested draw size. Stretching an
    ''' image inside DrawImage for every entity on every frame is expensive; scaling it
    ''' once and then blitting unscaled is not.
    ''' </summary>
    Private Shared Function GetScaledSprite(img As Image, w As Integer, h As Integer, keyPrefix As String) As Bitmap
        If img Is Nothing OrElse w <= 0 OrElse h <= 0 Then Return Nothing
        Dim key As String = $"{keyPrefix}_{w}x{h}"
        Dim cached As Bitmap = Nothing
        If spriteCache.TryGetValue(key, cached) Then Return cached

        Dim bmp As New Bitmap(w, h, PixelFormat.Format32bppPArgb)
        Using gs As Graphics = Graphics.FromImage(bmp)
            gs.InterpolationMode = InterpolationMode.NearestNeighbor
            gs.PixelOffsetMode = PixelOffsetMode.Half
            gs.SmoothingMode = SmoothingMode.None
            gs.DrawImage(img, New Rectangle(0, 0, w, h))
        End Using
        spriteCache(key) = bmp
        Return bmp
    End Function

    ''' <summary>Draw all bullets.</summary>
    Private Shared Sub RenderBullets(g As Graphics, engine As GameEngine)
        For Each b In engine.Bullets
            If engine.BulletImage IsNot Nothing AndAlso engine.BulletImage.Width > 0 Then
                Dim sprite = GetScaledSprite(engine.BulletImage, b.Width, b.Height, "bullet")
                If sprite IsNot Nothing Then g.DrawImageUnscaled(sprite, b.X, b.Y)
            Else
                ' Fallback: bright blue beam with glow (no per-entity allocations)
                g.FillEllipse(BulletGlowBrush, CSng(b.X - b.Width), CSng(b.Y - 4), CSng(b.Width * 3), CSng(b.Height + 8))
                g.FillRectangle(BulletCoreBrush, CSng(b.X), CSng(b.Y), CSng(b.Width), CSng(b.Height))
            End If
        Next
    End Sub

    ''' <summary>Draw all enemies.</summary>
    Private Shared Sub RenderEnemies(g As Graphics, engine As GameEngine)
        For Each ene In engine.Enemies
            If engine.EnemyImage IsNot Nothing AndAlso engine.EnemyImage.Width > 0 Then
                Dim sprite = GetScaledSprite(engine.EnemyImage, ene.Width, ene.Height, "enemy")
                If sprite IsNot Nothing Then
                    g.DrawImageUnscaled(sprite, CInt(Math.Round(ene.X)), CInt(Math.Round(ene.Y)))
                End If
            Else
                ' Fallback: red triangle (AA enabled only for this shape)
                Dim prevAA = g.SmoothingMode
                g.SmoothingMode = SmoothingMode.AntiAlias
                Dim pts() As PointF = {
                    New PointF(CSng(ene.X + ene.Width / 2), CSng(ene.Y + ene.Height)),
                    New PointF(CSng(ene.X), CSng(ene.Y)),
                    New PointF(CSng(ene.X + ene.Width), CSng(ene.Y))
                }
                g.FillPolygon(EnemyFillBrush, pts)
                g.DrawPolygon(EnemyPen, pts)
                g.SmoothingMode = prevAA
            End If
        Next
    End Sub

    ''' <summary>Draw the player ship.</summary>
    Private Shared Sub RenderPlayer(g As Graphics, engine As GameEngine)
        If Not engine.PlayerAlive Then Return

        Dim px = engine.PlayerX
        Dim py = engine.PlayerY
        Dim pw = GameEngine.PLAYER_WIDTH
        Dim ph = GameEngine.PLAYER_HEIGHT

        If engine.PlayerImage IsNot Nothing AndAlso engine.PlayerImage.Width > 0 Then
            Dim sprite = GetScaledSprite(engine.PlayerImage, pw, ph, "player")
            If sprite IsNot Nothing Then
                g.DrawImageUnscaled(sprite, CInt(Math.Round(px)), CInt(Math.Round(py)))
            End If

            ' Engine flame (animated) — reuse one brush, only mutate its colour
            Dim t As Double = Environment.TickCount64 / 50.0
            Dim flameH As Single = CSng(8 + Math.Sin(t) * 3)
            FlameBrush.Color = Color.FromArgb(255, 255, CByte(Math.Max(0, 150 + CInt(Math.Sin(t * 0.8) * 105))), 0)
            g.FillRectangle(FlameBrush, CSng(px + pw * 0.3), CSng(py + ph), CSng(pw * 0.4), flameH)
        Else
            ' Fallback: simple triangle shape (AA only for this shape)
            Dim prevAA = g.SmoothingMode
            g.SmoothingMode = SmoothingMode.AntiAlias
            Dim pts() As PointF = {
                New PointF(CSng(px + pw / 2), CSng(py)),
                New PointF(CSng(px), CSng(py + ph)),
                New PointF(CSng(px + pw), CSng(py + ph))
            }
            g.FillPolygon(PlayerFillBrush, pts)
            g.DrawPolygon(PlayerPen, pts)

            ' Engine flame
            FlameBrush.Color = Color.FromArgb(255, 255, 150, 0)
            g.FillRectangle(FlameBrush, CSng(px + pw * 0.3), CSng(py + ph), CSng(pw * 0.4), 8)
            g.SmoothingMode = prevAA
        End If
    End Sub

    ''' <summary>Draw explosion particles.</summary>
    Private Shared Sub RenderExplosions(g As Graphics, engine As GameEngine)
        If engine.Explosions.Count = 0 Then Return
        ' Particles are round, so they need AA — but it is toggled once per frame
        ' rather than per particle, and no brush is allocated per particle.
        Dim prevAA = g.SmoothingMode
        g.SmoothingMode = SmoothingMode.AntiAlias

        For Each p In engine.Explosions
            Dim progress As Double = 1.0 - CSng(p.Life) / (24.0 / 60.0) ' 0 = fresh, 1 = dead
            Dim alpha As Byte = CByte(Math.Max(0, Math.Min(255, p.Life * 60)))
            If alpha <= 0 Then Continue For

            Dim size As Single = CSng(progress * 30 + 2)
            ' Outer glow (red-orange)
            ParticleOuterBrush.Color = Color.FromArgb(alpha, 255, CByte(Math.Max(0, 150 - p.Life * 6)), 0)
            g.FillEllipse(ParticleOuterBrush, CSng(p.X - size), CSng(p.Y - size), size * 2, size * 2)

            ' Inner bright core (yellow-white)
            Dim innerSize As Single = size * 0.45F
            If innerSize > 1 Then
                ParticleInnerBrush.Color = Color.FromArgb(CByte(Math.Max(0, alpha - 30)), 255, 255, CByte(Math.Min(255, 200 + p.Life * 4)))
                g.FillEllipse(ParticleInnerBrush, CSng(p.X - innerSize), CSng(p.Y - innerSize), innerSize * 2, innerSize * 2)
            End If
        Next

        g.SmoothingMode = prevAA
    End Sub

    ''' <summary>Draw HUD overlay. Font/brush are cached; no MeasureString per frame.</summary>
    Private Shared Sub RenderHUD(g As Graphics, engine As GameEngine)
        g.DrawString($"Score: {engine.Score}", HudFont, WhiteBrush, 8, 8)
        g.DrawString($"Lives: {engine.Lives}", HudFont, WhiteBrush, CSng(engine.SurfaceWidth - LIVES_RIGHT_INSET), 8)
    End Sub

    ' Measured once (at most) and reused — MeasureString is GDI+ text layout and
    ' must not run 60 times a second.
    Private Shared livesRightInset As Single = -1

    Private Shared ReadOnly Property LIVES_RIGHT_INSET As Single
        Get
            If livesRightInset < 0 Then
                Dim dummy As New Bitmap(1, 1)
                Using gd As Graphics = Graphics.FromImage(dummy)
                    livesRightInset = gd.MeasureString("Lives: 000", HudFont).Width + 8
                End Using
            End If
            Return livesRightInset
        End Get
    End Property

    ''' <summary>Draw game over overlay (only rendered when the game is actually over).</summary>
    Private Shared Sub RenderGameOver(g As Graphics, engine As GameEngine)
        If Not engine.GameOver Then Return

        ' Darken overlay
        g.FillRectangle(OverlayBrush, 0, 0, CSng(engine.SurfaceWidth), CSng(engine.SurfaceHeight))

        Dim title As String = "GAME OVER"
        Dim final As String = $"Final Score: {engine.Score}"
        g.DrawString(title, GameOverFont, GameOverBrush,
                     CSng((engine.SurfaceWidth - GameOverTitleWidth(g, title)) / 2),
                     CSng(engine.SurfaceHeight / 2 - 30))
        g.DrawString(final, GameOverSubFont, GameOverSubBrush,
                     CSng((engine.SurfaceWidth - GameOverFinalWidth(g, final)) / 2),
                     CSng(engine.SurfaceHeight / 2 + 20))
    End Sub

    ' Game-over text metrics are measured once per distinct string, not once per frame.
    Private Shared gameOverTitleW As Single = -1
    Private Shared ReadOnly gameOverFinalWidths As New Dictionary(Of String, Single)(StringComparer.Ordinal)

    Private Shared Function GameOverTitleWidth(g As Graphics, text As String) As Single
        If gameOverTitleW < 0 Then gameOverTitleW = g.MeasureString(text, GameOverFont).Width
        Return gameOverTitleW
    End Function

    Private Shared Function GameOverFinalWidth(g As Graphics, text As String) As Single
        ' The string changes with the final score, so metrics are cached per string.
        Dim w As Single
        If gameOverFinalWidths.TryGetValue(text, w) Then Return w
        w = g.MeasureString(text, GameOverSubFont).Width
        gameOverFinalWidths(text) = w
        Return w
    End Function

End Class
