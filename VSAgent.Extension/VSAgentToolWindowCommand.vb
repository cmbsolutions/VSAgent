Imports System.ComponentModel.Design
Imports Microsoft.VisualStudio.Shell
Imports Microsoft.VisualStudio.Shell.Interop
Imports Task = System.Threading.Tasks.Task

''' <summary>
''' Command handler
''' </summary>
Public NotInheritable Class VSAgentToolWindowCommand

    ''' <summary>
    ''' Command ID.
    ''' </summary>
    Public Const CommandId As Integer = 4129

    ''' <summary>
    ''' Command menu group (command set GUID).
    ''' </summary>
    Public Shared ReadOnly CommandSet As New Guid("96a426bf-2a1f-424a-a8aa-b68720050973")

    ''' <summary>
    ''' VS Package that provides this command, not null.
    ''' </summary>
    Private ReadOnly package As AsyncPackage

    ''' <summary>
    ''' Initializes a new instance of the <see cref="VSAgentToolWindowCommand"/> class.
    ''' Adds our command handlers for menu (the commands must exist in the command table file)
    ''' </summary>
    ''' <param name="package">Owner package, not null.</param>
    Private Sub New(package As AsyncPackage, commandService As OleMenuCommandService)
        If package Is Nothing Then
            Throw New ArgumentNullException("package")
        End If

        If commandService Is Nothing Then
            Throw New ArgumentNullException(NameOf(commandService))
        End If

        Me.package = package

        Dim menuCommandId = New CommandID(CommandSet, CommandId)
        Dim menuCommand = New MenuCommand(AddressOf Me.Execute, menuCommandId)
        commandService.AddCommand(menuCommand)
    End Sub

    ''' <summary>
    ''' Gets the instance of the command.
    ''' </summary>
    Public Shared Property Instance As VSAgentToolWindowCommand

    ''' <summary>
    ''' Get service provider from the owner package.
    ''' </summary>
    Private ReadOnly Property ServiceProvider As Microsoft.VisualStudio.Shell.IAsyncServiceProvider
        Get
            Return Me.package
        End Get
    End Property

    ''' <summary>
    ''' Initializes the singleton instance of the command.
    ''' </summary>
    ''' <param name="package">Owner package, Not null.</param>
    Public Shared Async Function InitializeAsync(package As AsyncPackage) As Task
        ' Switch to the main thread - the call to AddCommand in VSAgentToolWindowCommand's constructor requires
        ' the UI thread.
        Await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken)

        Dim commandService As OleMenuCommandService = Await package.GetServiceAsync(GetType(IMenuCommandService))
        Instance = New VSAgentToolWindowCommand(package, commandService)
    End Function

    ''' <summary>
    ''' Shows the tool window when the menu item is clicked.
    ''' </summary>
    ''' <param name="sender">The event sender.</param>
    ''' <param name="e">The event args.</param>
    Private Sub Execute(sender As Object, e As EventArgs)
        Dim unused = Me.package.JoinableTaskFactory.RunAsync(Async Function()
                                                                 Dim window As ToolWindowPane = Await Me.package.ShowToolWindowAsync(GetType(VSAgentToolWindow), 0, True, Me.package.DisposalToken)
                                                                 If window Is Nothing OrElse window.Frame Is Nothing Then
                                                                     Throw New NotSupportedException("Cannot create tool window")
                                                                 End If

                                                                 Await Me.package.JoinableTaskFactory.SwitchToMainThreadAsync()

                                                                 Dim windowFrame As IVsWindowFrame = window.Frame
                                                                 Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show())
                                                             End Function)
    End Sub
End Class
