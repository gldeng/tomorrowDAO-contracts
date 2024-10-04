using System;
using System.Linq;
using AElf.CSharp.Core;
using AElf.CSharp.Core.Extension;
using AElf.Types;
using Google.Protobuf;

namespace TomorrowDAO.Contracts.Vote;

public static class Extenstions
{
    public static bool ShouldContainEvent<TEvent>(this IExecutionTask task, TEvent @event, Address address)
        where TEvent : IEvent<TEvent>, IMessage<TEvent>, new()
    {
        var le = @event.ToLogEvent(address);
        // ReSharper disable once ComplexConditionExpression
        var found = task.TransactionResult.Logs.FirstOrDefault(l =>
            (address == null || address == l.Address) && le.Indexed.All(topic => l.Indexed.Contains(topic))
        );
        if (found == null)
        {
            throw new Exception("Event with the correct topic is not found.");
        }

        var foundEvent = new TEvent();
        foundEvent.MergeFrom(found.NonIndexed);

        if (!@event.GetNonIndexed().Equals(foundEvent))
        {
            throw new Exception($"Event not found:\nExpected: {@event.GetNonIndexed()}\nActual: {foundEvent}");
        }

        return true;
    }
}