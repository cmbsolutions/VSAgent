Imports Microsoft.CodeAnalysis
Imports Microsoft.VisualStudio.Package
Imports Microsoft.VisualStudio.Shell
Imports VSAgent.Protocol.DTO

Public Class VisualStudioRoslynWorkspaceService
    Implements IRoslynWorkspaceService

    Private ReadOnly _package As AsyncPackage
    Private ReadOnly _excludedFolders As New List(Of String)({"bin", "obj", ".vs", ".git", ".svn"})

    Public Sub New(package As AsyncPackage)

        If package Is Nothing Then
            Throw New ArgumentNullException(NameOf(package))
        End If

        _package = package
    End Sub

    Public Async Function GetProjectsAsync() As Task(Of IReadOnlyList(Of RoslynProjectInfo)) Implements IRoslynWorkspaceService.GetProjectsAsync

        Dim workspace = Await RoslynWorkspaceProvider.GetWorkspaceAsync(_package)

        Dim result As New List(Of RoslynProjectInfo)

        For Each project As Project In workspace.CurrentSolution.Projects

            result.Add(
                New RoslynProjectInfo With {
                    .Id = project.Id.Id.ToString(),
                    .Name = project.Name,
                    .AssemblyName = project.AssemblyName,
                    .Language = project.Language,
                    .FilePath = project.FilePath,
                    .OutputFilePath = project.OutputFilePath,
                    .DocumentCount = project.DocumentIds.Count,
                    .ProjectReferenceCount = project.ProjectReferences.Count(),
                    .MetadataReferenceCount = project.MetadataReferences.Count()
                })

        Next

        Return result

    End Function

    Public Async Function GetProjectDocumentsAsync(projectId As String) As Task(Of IReadOnlyList(Of RoslynDocumentInfo)) Implements IRoslynWorkspaceService.GetProjectDocumentsAsync
        Dim workspace = Await RoslynWorkspaceProvider.GetWorkspaceAsync(_package)

        Dim result As New List(Of RoslynDocumentInfo)

        Dim solution = workspace.CurrentSolution

        Dim project = solution.Projects.FirstOrDefault(
            Function(p)
                Return String.Equals(p.Id.Id.ToString(), projectId, StringComparison.OrdinalIgnoreCase)
            End Function)

        If project Is Nothing Then
            Throw New InvalidOperationException("The requested project could not be found.")
        End If

        For Each document As Document In project.Documents
            result.Add(
                New RoslynDocumentInfo With {
                    .DocumentID = document.Id.Id.ToString(),
                    .Name = document.Name,
                    .FilePath = document.FilePath,
                    .ProjectID = document.Project.Id.Id.ToString(),
                    .Folders = document.Folders.ToList
                })
        Next

        Return result
    End Function

    Public Async Function GetProjectsFilesAsync(projectId As String) As Task(Of IReadOnlyList(Of ProjectFileInfo)) Implements IRoslynWorkspaceService.GetProjectsFilesAsync
        Dim workspace = Await RoslynWorkspaceProvider.GetWorkspaceAsync(_package)

        Dim result As New List(Of ProjectFileInfo)

        Dim solution = workspace.CurrentSolution

        Dim project = solution.Projects.FirstOrDefault(
            Function(p)
                Return String.Equals(p.Id.Id.ToString(), projectId, StringComparison.OrdinalIgnoreCase)
            End Function)

        If project Is Nothing Then
            Throw New InvalidOperationException("The requested project could not be found.")
        End If

        Dim ProjectFolder = New IO.DirectoryInfo(IO.Path.GetDirectoryName(project.FilePath))

        FindFilesRecursively(ProjectFolder, result)

        Return result
    End Function

    Private Sub FindFilesRecursively(folder As IO.DirectoryInfo, result As List(Of ProjectFileInfo))
        Try
            For Each file In folder.EnumerateFileSystemInfos
                result.Add(
                New ProjectFileInfo With {
                    .FileChecksum = file.GetHashCode().ToString(),
                    .Name = file.Name,
                    .FilePath = file.FullName,
                    .RelativePath = file.FullName.Substring(folder.FullName.Length).TrimStart(IO.Path.DirectorySeparatorChar),
                    .Type = file.Extension,
                    .Version = file.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss")
                })
            Next

            For Each subFolder In folder.EnumerateDirectories()
                If _excludedFolders.Contains(subFolder.Name, StringComparer.OrdinalIgnoreCase) Then
                    Continue For
                End If

                If subFolder.Attributes = IO.FileAttributes.ReparsePoint Then
                    Continue For
                End If

                FindFilesRecursively(subFolder, result)
            Next
        Catch ex As Exception
            ' Handle exceptions (e.g., access denied) if necessary
        End Try
    End Sub

    Public Async Function InspectTypeAsync(projectId As String, typeName As String, memberName As String) As Task(Of InspectTypeInfo) Implements IRoslynWorkspaceService.InspectTypeAsync
        Dim workspace = Await RoslynWorkspaceProvider.GetWorkspaceAsync(_package)

        Dim result As New InspectTypeInfo

        Dim solution = workspace.CurrentSolution

        Dim project = solution.Projects.FirstOrDefault(
            Function(p)
                Return String.Equals(p.Id.Id.ToString(), projectId, StringComparison.OrdinalIgnoreCase)
            End Function)

        If project Is Nothing Then
            Throw New InvalidOperationException("The requested project could not be found.")
        End If

        Dim compilation = Await project.GetCompilationAsync()

        Dim typeSymbol = compilation.GetTypeByMetadataName(typeName)

        If typeSymbol Is Nothing Then
            Throw New InvalidOperationException("The requested typeName could not be found.")
        End If

        Dim displayFormat As SymbolDisplayFormat = SymbolDisplayFormat.VisualBasicErrorMessageFormat

        Select Case project.Language
            Case LanguageNames.CSharp
                displayFormat = SymbolDisplayFormat.CSharpErrorMessageFormat
            Case LanguageNames.VisualBasic
                displayFormat = SymbolDisplayFormat.VisualBasicErrorMessageFormat
        End Select

        result.TypeName = typeSymbol.ToDisplayString(displayFormat)
        result.BaseType = If(typeSymbol.BaseType IsNot Nothing, typeSymbol.BaseType.ToDisplayString(displayFormat), String.Empty)
        result.Assembly = typeSymbol.ContainingAssembly.ToDisplayString(displayFormat)

        Dim currentType As INamedTypeSymbol = typeSymbol

        While currentType IsNot Nothing
            For Each memberSymbol In currentType.GetMembers()

                If Not String.IsNullOrWhiteSpace(memberName) AndAlso Not String.Equals(memberSymbol.Name, memberName, StringComparison.OrdinalIgnoreCase) Then
                    Continue For
                End If

                If memberSymbol.DeclaredAccessibility = Accessibility.Public OrElse
                    memberSymbol.DeclaredAccessibility = Accessibility.Protected OrElse
                    memberSymbol.DeclaredAccessibility = Accessibility.ProtectedOrInternal Then

                    Dim methodSymbol = TryCast(memberSymbol, IMethodSymbol)

                    result.Members.Add(New InspectTypeMemberInfo With {
                            .Name = memberSymbol.Name,
                            .Kind = memberSymbol.Kind.ToString(),
                            .Signature = memberSymbol.ToDisplayString(displayFormat),
                            .DeclaredIn = currentType.ToDisplayString(displayFormat),
                            .IsOverridable = methodSymbol IsNot Nothing AndAlso (methodSymbol.IsVirtual OrElse methodSymbol.IsAbstract OrElse methodSymbol.IsOverride) AndAlso Not methodSymbol.IsSealed
                        })

                End If
            Next
            If memberName <> "" Then
                currentType = currentType.BaseType
            Else
                currentType = Nothing
            End If
        End While

        Return result
    End Function
End Class