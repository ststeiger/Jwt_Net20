//
// Enumerable.cs
//
// Authors:
//  Marek Safar (marek.safar@gmail.com)
//  Antonello Provenzano  <antonello@deveel.com>
//  Alejandro Serrano "Serras" (trupill@yahoo.es)
//  Jb Evain (jbevain@novell.com)
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

// precious: http://www.hookedonlinq.com

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace JWT.PetaJson
{
    internal static class Enumerable
    {
        enum Fallback {
            Default,
            Throw
        }




        static class PredicateOf<T> {
            public static readonly ReadCallback_t<T, bool> Always = (t) => true;
        }



        #region All

        public static bool All<TSource>(this IEnumerable<TSource> source, ReadCallback_t<TSource, bool> predicate)
        {
            Check.SourceAndPredicate (source, predicate);

            foreach (TSource element in source)
                if (!predicate (element))
                    return false;

            return true;
        }

        #endregion

        #region Any

        public static bool Any<TSource> (this IEnumerable<TSource> source)
        {
            Check.Source (source);

            ICollection<TSource> collection = source as ICollection<TSource>;
            if (collection != null)
                return collection.Count > 0;

            using (System.Collections.Generic.IEnumerator<TSource> enumerator = source.GetEnumerator())
                return enumerator.MoveNext ();
        }

        public static bool Any<TSource>(this IEnumerable<TSource> source, ReadCallback_t<TSource, bool> predicate)
        {
            Check.SourceAndPredicate (source, predicate);

            foreach (TSource element in source)
                if (predicate (element))
                    return true;

            return false;
        }

        #endregion



        #region First

        static TSource First<TSource>(this IEnumerable<TSource> source, ReadCallback_t<TSource, bool> predicate, Fallback fallback)
        {
            foreach (TSource element in source)
                if (predicate (element))
                    return element;

            if (fallback == Fallback.Throw)
                throw NoMatchingElement ();

            return default (TSource);
        }

        public static TSource First<TSource> (this IEnumerable<TSource> source)
        {
            Check.Source (source);

            IList<TSource> list = source as IList<TSource>;
            if (list != null) {
                if (list.Count != 0)
                    return list [0];
            } else {
                using (System.Collections.Generic.IEnumerator<TSource> enumerator = source.GetEnumerator())
                {
                    if (enumerator.MoveNext ())
                        return enumerator.Current;
                }
            }

            throw EmptySequence ();
        }

        public static TSource First<TSource>(this IEnumerable<TSource> source, ReadCallback_t<TSource, bool> predicate)
        {
            Check.SourceAndPredicate (source, predicate);

            return source.First (predicate, Fallback.Throw);
        }

        #endregion

        #region FirstOrDefault

        public static TSource FirstOrDefault<TSource> (this IEnumerable<TSource> source)
        {
            Check.Source (source);

            #if !FULL_AOT_RUNTIME
            return source.First (PredicateOf<TSource>.Always, Fallback.Default);
            #else
            // inline the code to reduce dependency o generic causing AOT errors on device (e.g. bug #3285)
            foreach (TSource element in source)
            return element;

            return default (TSource);
            #endif
        }

        public static TSource FirstOrDefault<TSource>(this IEnumerable<TSource> source, ReadCallback_t<TSource, bool> predicate)
        {
            Check.SourceAndPredicate (source, predicate);

            return source.First (predicate, Fallback.Default);
        }

        #endregion


        #region OfType

        public static IEnumerable<TResult> OfType<TResult> (this IEnumerable source)
        {
            Check.Source (source);

            return CreateOfTypeIterator<TResult> (source);
        }

        static IEnumerable<TResult> CreateOfTypeIterator<TResult> (IEnumerable source)
        {
            foreach (object element in source)
                if (element is TResult)
                    yield return (TResult) element;
        }

        #endregion


        #region Exception helpers

        static Exception EmptySequence ()
        {
            return new InvalidOperationException ( ("Sequence contains no elements"));
        }
        static Exception NoMatchingElement ()
        {
            return new InvalidOperationException ( ("Sequence contains no matching element"));
        }
        #endregion
    }
}
