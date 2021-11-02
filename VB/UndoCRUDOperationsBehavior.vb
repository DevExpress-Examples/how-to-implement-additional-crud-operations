Imports DevExpress.Mvvm
Imports DevExpress.Mvvm.UI.Interactivity
Imports DevExpress.Mvvm.Xpf
Imports DevExpress.Xpf.Grid
Imports System
Imports System.Collections
Imports System.Threading
Imports System.Windows
Imports System.Windows.Input

Namespace UndoOperation

    Public Class UndoCRUDOperationsBehavior
        Inherits Behavior(Of TableView)

        Private _UndoCommand As ICommand

        Public Shared ReadOnly CopyOperationsSupporterProperty As DependencyProperty

        Shared Sub New()
            CopyOperationsSupporterProperty = DependencyProperty.Register(NameOf(UndoCRUDOperationsBehavior.CopyOperationsSupporter), GetType(ICopyOperationSupporter), GetType(UndoCRUDOperationsBehavior), New PropertyMetadata(CType(Nothing, PropertyChangedCallback)))
        End Sub

        Private ReadOnly Property Source As IList
            Get
                Return CType(AssociatedObject.DataControl.ItemsSource, IList)
            End Get
        End Property

        Private undoAction As Action

        Private isNewItemRowEditing As Boolean

        Private editingCache As Object

        Public Property CopyOperationsSupporter As ICopyOperationSupporter
            Get
                Return CType(GetValue(CopyOperationsSupporterProperty), ICopyOperationSupporter)
            End Get

            Set(ByVal value As ICopyOperationSupporter)
                SetValue(CopyOperationsSupporterProperty, value)
            End Set
        End Property

        Public Property UndoCommand As ICommand
            Get
                Return _UndoCommand
            End Get

            Private Set(ByVal value As ICommand)
                _UndoCommand = value
            End Set
        End Property

        Public Sub New()
            UndoCommand = New DelegateCommand(AddressOf Undo, AddressOf CanUndo)
        End Sub

        Protected Overrides Sub OnAttached()
            MyBase.OnAttached()
            AssociatedObject.ValidateRow += AddressOf OnRowAddedOrEdited
            AssociatedObject.ValidateRowDeletion += AddressOf OnRowDeleted
            AssociatedObject.RowEditStarted += AddressOf OnEditingStarted
            AssociatedObject.DataSourceRefresh += AddressOf OnRefresh
            AssociatedObject.InitNewRow += AddressOf OnNewRowStarted
        End Sub

        Protected Overrides Sub OnDetaching()
            AssociatedObject.ValidateRow -= AddressOf OnRowAddedOrEdited
            AssociatedObject.ValidateRowDeletion -= AddressOf OnRowDeleted
            AssociatedObject.RowEditStarted -= AddressOf OnEditingStarted
            AssociatedObject.DataSourceRefresh -= AddressOf OnRefresh
            AssociatedObject.InitNewRow -= AddressOf OnNewRowStarted
            MyBase.OnDetaching()
        End Sub

        Private Sub OnRefresh(ByVal sender As Object, ByVal e As DataSourceRefreshEventArgs)
            Clear()
        End Sub

        Private Sub Clear()
            undoAction = Nothing
            editingCache = Nothing
        End Sub

        Private Sub OnNewRowStarted(ByVal sender As Object, ByVal e As InitNewRowEventArgs)
            isNewItemRowEditing = True
        End Sub

        Private Sub Undo()
            undoAction?.Invoke()
            undoAction = Nothing
        End Sub

        Private Function CanUndo() As Boolean
            Return undoAction IsNot Nothing AndAlso Not AssociatedObject.IsEditing AndAlso Not isNewItemRowEditing AndAlso Not AssociatedObject.IsDataSourceRefreshing
        End Function

        Private Sub UndoEditAction(ByVal item As Object)
            ApplyEditingCache(item)
            AssociatedObject.DataControl.CurrentItem = item
        End Sub

        Private Sub UndoAddAction(ByVal item As Object)
            AssociatedObject.DataControl.CurrentItem = item
            RemoveItem(item)
        End Sub

        Private Sub UndoDeleteAction(ByVal position As Integer, ByVal item As Object)
            InsertItem(position, item)
            AssociatedObject.DataControl.CurrentItem = item
        End Sub

        Private Sub OnEditingStarted(ByVal sender As Object, ByVal e As RowEditStartedEventArgs)
            If e.RowHandle IsNot DataControlBase.NewItemRowHandle Then
                editingCache = CopyOperationsSupporter.Clone(e.Row)
            End If
        End Sub

        Private Sub OnRowDeleted(ByVal sender As Object, ByVal e As GridValidateRowDeletionEventArgs)
            undoAction = New Action(Sub() UndoDeleteAction(e.RowHandles.[Single](), CopyOperationsSupporter.Clone(e.Rows.[Single]())))
        End Sub

        Private Sub OnRowAddedOrEdited(ByVal sender As Object, ByVal e As GridRowValidationEventArgs)
            Dim item = e.Row
            Dim isNewItem = e.IsNewItem
            undoAction = If(e.IsNewItem, New Action(Sub() UndoAddAction(item)), New Action(Sub() UndoEditAction(item)))
            isNewItemRowEditing = False
        End Sub

        Private Sub ApplyEditingCache(ByVal item As Object)
            CopyOperationsSupporter.CopyTo(editingCache, item)
            editingCache = Nothing
            AssociatedObject.DataControl.RefreshRow(AssociatedObject.DataControl.FindRow(item))
            AssociatedObject.ValidateRowCommand?.Execute(New RowValidationArgs(editingCache, Source.IndexOf(item), False, New CancellationToken(), False))
        End Sub

        Private Sub RemoveItem(ByVal item As Object)
            Source.Remove(item)
            AssociatedObject.ValidateRowDeletionCommand?.Execute(New ValidateRowDeletionArgs(New Object() {item}, New Integer() {Source.IndexOf(item)}))
        End Sub

        Private Sub InsertItem(ByVal position As Integer, ByVal item As Object)
            Source.Insert(position, item)
            AssociatedObject.ValidateRowCommand?.Execute(New RowValidationArgs(item, Source.IndexOf(item), True, New CancellationToken(), False))
        End Sub
    End Class
End Namespace
