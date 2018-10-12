namespace KGySoft.ComponentModel
{
    internal interface ICanUndoInternal
    {
        void SuspendUndo();
        void ResumeUndo();
        void ClearUndoHistory();
    }
}