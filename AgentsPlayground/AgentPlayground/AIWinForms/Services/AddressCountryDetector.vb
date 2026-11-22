Imports System.Globalization
Imports System.Text
Imports System.Text.RegularExpressions
Imports MySqlConnector

Public Class AddressCountryDetector

    '--- Alias entry used for matching ---
    Private Class AliasEntry
        Public Property CountryId As String
        Public Property AliasText As String
        Public Property Normalized As String
        Public Property Tokens As List(Of String)
        Public Property Length As Integer
    End Class

    '--- Result of a detection run ---
    Public Class DetectionResult
        Public Property CountryId As String
        Public Property Score As Double
        Public Property MatchedAliases As List(Of String)
        Public Property Confidence As DetectionConfidence
    End Class

    Public Enum DetectionConfidence
        None = 0
        Low = 1
        Medium = 2
        High = 3
    End Enum

    '--- Cached alias data ---
    Private _aliases As List(Of AliasEntry) = Nothing
    Private _aliasesLoaded As Boolean = False
    Private _lockObject As Object = New Object()

    '--- Weighting constants ---
    Private Const WeightExactMatch As Double = 10.0
    Private Const WeightContainsMatch As Double = 5.0
    Private Const WeightTokenMatch As Double = 2.0
    Private Const MultiLineBonus As Double = 3.0
    Private Const MinScoreThreshold As Double = 4.0

    '============================================================
    '  PUBLIC API
    '============================================================

    ''' <summary>
    ''' Determines the most likely country for the given address lines.
    ''' </summary>
    ''' <param name="addressLines">Array of strings representing the address (any language).</param>
    ''' <returns>A DetectionResult with the best-matching country_id, or CountryId = Nothing if no match.</returns>
    Public Function DetectCountry(ParamArray addressLines() As String) As DetectionResult
        Return DetectCountry(addressLines.ToList())
    End Function

    ''' <summary>
    ''' Determines the most likely country for the given address lines.
    ''' </summary>
    Public Function DetectCountry(lines As IList(Of String)) As DetectionResult
        If lines Is Nothing OrElse lines.Count = 0 Then
            Return New DetectionResult With {
                .CountryId = Nothing,
                .Score = 0.0,
                .MatchedAliases = New List(Of String),
                .Confidence = DetectionConfidence.None
            }
        End If

        EnsureAliasesLoaded()

        ' Build a normalized combined text and per-line normalized texts
        Dim normalizedLines As New List(Of String)()
        For Each line In lines
            If Not String.IsNullOrWhiteSpace(line) Then
                normalizedLines.Add(Normalize(line))
            End If
        Next

        If normalizedLines.Count = 0 Then
            Return New DetectionResult With {
                .CountryId = Nothing,
                .Score = 0.0,
                .MatchedAliases = New List(Of String),
                .Confidence = DetectionConfidence.None
            }
        End If

        ' Score each alias against the address using a Max-Score Strategy to prevent fragmentation cheating.
        Dim scores As New Dictionary(Of String, AliasScore)()

        For Each entry In _aliases
            ' Find the single best matching line for this specific alias
            Dim bestLineScore As Double = 0.0
            Dim matchedAliasText As String = Nothing

            For Each normLine In normalizedLines
                Dim lineScore As Double = ScoreLine(entry, normLine)
                If lineScore > bestLineScore Then
                    bestLineScore = lineScore
                    matchedAliasText = entry.AliasText
                End If
            Next

            ' Only update if we have a match and it beats any previous alias for this country
            If bestLineScore > 0 Then
                If scores.ContainsKey(entry.CountryId) Then
                    If bestLineScore > scores(entry.CountryId).TotalScore Then
                        scores(entry.CountryId) = New AliasScore With {
                            .TotalScore = bestLineScore,
                            .MatchedAliases = New List(Of String) From {matchedAliasText}
                        }
                    End If
                Else
                    scores(entry.CountryId) = New AliasScore With {
                        .TotalScore = bestLineScore,
                        .MatchedAliases = New List(Of String) From {matchedAliasText}
                    }
                End If
            End If
        Next

        ' Pick the best country based on the highest single alias match
        Dim bestCountry As String = Nothing
        Dim bestScore As Double = 0.0
        Dim bestAliases As New List(Of String)()

        For Each kvp In scores
            If kvp.Value.TotalScore > bestScore Then
                bestScore = kvp.Value.TotalScore
                bestCountry = kvp.Key
                bestAliases = kvp.Value.MatchedAliases
            End If
        Next

        If bestCountry Is Nothing OrElse bestScore < MinScoreThreshold Then
            Return New DetectionResult With {
                .CountryId = Nothing,
                .Score = bestScore,
                .MatchedAliases = New List(Of String),
                .Confidence = DetectionConfidence.None
            }
        End If

        ' Determine confidence
        Dim confidence As DetectionConfidence
        If bestScore >= 30.0 Then
            confidence = DetectionConfidence.High
        ElseIf bestScore >= 15.0 Then
            confidence = DetectionConfidence.Medium
        Else
            confidence = DetectionConfidence.Low
        End If

        Return New DetectionResult With {
            .CountryId = bestCountry,
            .Score = bestScore,
            .MatchedAliases = bestAliases,
            .Confidence = confidence
        }
    End Function

    ''' <summary>
    ''' Returns all countries that matched, sorted by score descending.
    ''' </summary>
    Public Function DetectAllCountries(lines As IList(Of String)) As List(Of DetectionResult)
        If lines Is Nothing OrElse lines.Count = 0 Then
            Return New List(Of DetectionResult)()
        End If

        EnsureAliasesLoaded()

        Dim normalizedLines As New List(Of String)()
        For Each line In lines
            If Not String.IsNullOrWhiteSpace(line) Then
                normalizedLines.Add(Normalize(line))
            End If
        Next

        If normalizedLines.Count = 0 Then Return New List(Of DetectionResult)()

        Dim scores As New Dictionary(Of String, AliasScore)()

        For Each entry In _aliases
            ' Find the single best matching line for this specific alias (Max-Score Strategy)
            Dim bestLineScore As Double = 0.0
            Dim matchedAliasText As String = Nothing

            For Each normLine In normalizedLines
                Dim lineScore As Double = ScoreLine(entry, normLine)
                If lineScore > bestLineScore Then
                    bestLineScore = lineScore
                    matchedAliasText = entry.AliasText
                End If
            Next

            If bestLineScore > 0 Then
                If scores.ContainsKey(entry.CountryId) Then
                    If bestLineScore > scores(entry.CountryId).TotalScore Then
                        scores(entry.CountryId) = New AliasScore With {
                            .TotalScore = bestLineScore,
                            .MatchedAliases = New List(Of String) From {matchedAliasText}
                        }
                    End If
                Else
                    scores(entry.CountryId) = New AliasScore With {
                        .TotalScore = bestLineScore,
                        .MatchedAliases = New List(Of String) From {matchedAliasText}
                    }
                End If
            End If
        Next

        Return scores.OrderByDescending(Function(k) k.Value.TotalScore) _
            .Select(Function(k) New DetectionResult With {
                .CountryId = k.Key,
                .Score = k.Value.TotalScore,
                .MatchedAliases = k.Value.MatchedAliases,
                .Confidence = GetConfidence(k.Value.TotalScore)
            }).ToList()
    End Function

    ''' <summary>
    ''' Forces a reload of aliases from the database.
    ''' </summary>
    Public Sub ReloadAliases()
        SyncLock _lockObject
            _aliasesLoaded = False
            _aliases = Nothing
            EnsureAliasesLoaded()
        End SyncLock
    End Sub

    '============================================================
    '  PRIVATE HELPERS
    '============================================================

    Private Class AliasScore
        Public Property TotalScore As Double
        Public Property MatchedAliases As List(Of String)
    End Class

    Private Sub EnsureAliasesLoaded()
        If _aliasesLoaded Then Return
        SyncLock _lockObject
            If _aliasesLoaded Then Return
            LoadAliases()
            _aliasesLoaded = True
        End SyncLock
    End Sub

    Private Sub LoadAliases()
        _aliases = New List(Of AliasEntry)()

        Try
            Using conn As New MySqlConnection(MySQLConnectionString.connectionString)
                conn.Open()
                Using cmd As New MySqlCommand("SELECT country_id, [alias], alias_normalized FROM countryaliases", conn)
                    Using reader As MySqlDataReader = cmd.ExecuteReader()
                        While reader.Read()
                            Dim countryId As String = reader.GetString(0)
                            Dim aliasValue As String = reader.GetString(1)
                            Dim normalized As String
                            If reader.IsDBNull(2) Then
                                normalized = Normalize(aliasValue)
                            Else
                                normalized = reader.GetString(2).ToLowerInvariant()
                            End If

                            _aliases.Add(New AliasEntry With {
                                .CountryId = countryId,
                                .AliasText = aliasValue,
                                .Normalized = normalized,
                                .Tokens = Tokenize(normalized),
                                .Length = normalized.Length
                            })
                        End While
                    End Using
                End Using
            End Using
        Catch ex As MySqlException
            ' If the DB is unavailable we throw so the caller knows.
            Throw New InvalidOperationException("Failed to load country aliases from database.", ex)
        Catch ex As Exception
            Throw New InvalidOperationException("Unexpected error loading country aliases.", ex)
        End Try
    End Sub

    ''' <summary>
    ''' Scores how well a single alias entry matches a single normalized address line using mutually exclusive tiers.
    ''' </summary>
    Private Function ScoreLine(entry As AliasEntry, normalizedLine As String) As Double
        ' 1. Exact match (alias equals the entire line)
        If normalizedLine = entry.Normalized Then
            Return WeightExactMatch + (entry.Length * 0.2)
        End If

        ' 2. Contains match (Alias is a substring of line, or vice versa)
        If normalizedLine.Contains(entry.Normalized) Then
            Return WeightContainsMatch + (entry.Length * 0.1)
        ElseIf entry.Normalized.Contains(normalizedLine) AndAlso normalizedLine.Length >= 3 Then
            ' The address line is a subset of the alias (e.g. "NL" matching "Netherlands")
            Return (WeightContainsMatch * 0.5) + (entry.Length * 0.1)
        End If

        ' 3. Token-level matching (Requires all tokens to be present in the line)
        ' We check this only if it's not a substring match, as substring is usually stronger for names.
        If entry.Tokens.Count > 1 Then
            Dim lineTokens As List(Of String) = Tokenize(normalizedLine)
            ' Check if every token from the alias exists in the address line
            Dim allPresent As Boolean = True
            For Each tok In entry.Tokens
                If Not lineTokens.Contains(tok) Then
                    allPresent = False
                    Exit For
                End If
            Next

            If allPresent Then
                Return WeightTokenMatch * entry.Tokens.Count + (entry.Length * 0.15)
            End If

            ' Partial token match: at least half of the words are there
            Dim tokensFound As Integer = entry.Tokens.Where(Function(t) lineTokens.Contains(t)).Count()
            If tokensFound >= (entry.Tokens.Count / 2) Then
                Return WeightTokenMatch * tokensFound + (entry.Length * 0.1)
            End If
        End If

        ' 4. Single-token alias: check if that token appears as a whole word in the line
        If entry.Tokens.Count = 1 Then
            Dim tok As String = entry.Tokens(0)
            If tok.Length >= 2 AndAlso Regex.IsMatch(normalizedLine, "(^|\s)" & Regex.Escape(tok) & "(\s|$)") Then
                Return WeightTokenMatch + (tok.Length * 0.2)
            End If
        End If

        Return 0.0
    End Function

    ''' <summary>
    ''' Normalizes text: lowercase, remove diacritics, trim.
    ''' </summary>
    Public Shared Function Normalize(text As String) As String
        If String.IsNullOrEmpty(text) Then Return String.Empty
        Dim sb As New StringBuilder(text.Length)
        Dim normalizedChars As Char() = text.Normalize(NormalizationForm.FormD).ToCharArray()
        For Each c In normalizedChars
            Dim category As UnicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c)
            If category <> UnicodeCategory.NonSpacingMark Then
                sb.Append(c)
            End If
        Next
        Return sb.ToString().ToLowerInvariant().Trim()
    End Function

    ''' <summary>
    ''' Splits normalized text into lowercase alphanumeric tokens of length >= 2.
    ''' </summary>
    Private Shared Function Tokenize(text As String) As List(Of String)
        Dim matches As MatchCollection = Regex.Matches(text, "[a-z0-9]{2,}")
        Dim tokens As New List(Of String)()
        For Each m As Match In matches
            tokens.Add(m.Value)
        Next
        Return tokens
    End Function

    Private Function GetConfidence(score As Double) As DetectionConfidence
        If score >= 30.0 Then Return DetectionConfidence.High
        If score >= 15.0 Then Return DetectionConfidence.Medium
        If score >= MinScoreThreshold Then Return DetectionConfidence.Low
        Return DetectionConfidence.None
    End Function

End Class
