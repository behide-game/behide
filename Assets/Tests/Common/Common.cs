using System;
using UnityEngine;

public class Common
{
    static public CustomYieldInstruction waitUntilOrTimeout(int timeout, Func<bool> predicate)
    {
        DateTimeOffset startTime = DateTimeOffset.Now;
        return new WaitUntil(() =>
        {
            TimeSpan timeSpan = DateTimeOffset.Now - startTime;
            bool timeoutExceeded = timeSpan.TotalMilliseconds >= timeout;

            return timeoutExceeded || predicate();
        });
    }
}
