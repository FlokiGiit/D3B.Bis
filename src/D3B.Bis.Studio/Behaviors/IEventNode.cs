namespace D3B.Bis.Studio.Behaviors;

/// <summary>An event's code, its values boxed, as the run calls it.</summary>
internal interface IEventNode
{
    bool Happened(BotChange change, object values);
}
