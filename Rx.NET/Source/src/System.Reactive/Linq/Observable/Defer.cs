﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information. 

using System;
using System.Reactive.Disposables;

namespace System.Reactive.Linq.ObservableImpl
{
    internal sealed class Defer<TValue> : Producer<TValue>, IEvaluatableObservable<TValue>
    {
        private readonly Func<IObservable<TValue>> _observableFactory;

        public Defer(Func<IObservable<TValue>> observableFactory)
        {
            _observableFactory = observableFactory;
        }

        protected override IDisposable Run(IObserver<TValue> observer, IDisposable cancel, Action<IDisposable> setSink)
        {
            var sink = new _(this, observer, cancel);
            setSink(sink);
            return sink.Run();
        }

        public IObservable<TValue> Eval()
        {
            return _observableFactory();
        }

        class _ : Sink<TValue>, IObserver<TValue>
        {
            private readonly Defer<TValue> _parent;

            public _(Defer<TValue> parent, IObserver<TValue> observer, IDisposable cancel)
                : base(observer, cancel)
            {
                _parent = parent;
            }

            public IDisposable Run()
            {
                var result = default(IObservable<TValue>);
                try
                {
                    result = _parent.Eval();
                }
                catch (Exception exception)
                {
                    base._observer.OnError(exception);
                    base.Dispose();
                    return Disposable.Empty;
                }

                return result.SubscribeSafe(this);
            }

            public void OnNext(TValue value)
            {
                base._observer.OnNext(value);
            }

            public void OnError(Exception error)
            {
                base._observer.OnError(error);
                base.Dispose();
            }

            public void OnCompleted()
            {
                base._observer.OnCompleted();
                base.Dispose();
            }
        }
    }
}
