// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Microsoft.Azure.WebJobs.Script.Eventing
{
    public class ScriptEventManager
    {
        private static readonly Subject<IScriptEvent> _subject = new Subject<IScriptEvent>();

        public IObservable<IScriptEvent> Events => _subject.AsObservable();

        public void Publish(IScriptEvent scriptEvent)
        {
            _subject.OnNext(scriptEvent);
        }
    }
}
