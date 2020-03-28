// Copyright Neal Gafter 2019.

//This program is free software: you can redistribute it and/or modify
//it under the terms of the GNU General Public License as published by
//the Free Software Foundation, either version 3 of the License, or
//(at your option) any later version.

//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//GNU General Public License for more details.

//You should have received a copy of the GNU General Public License
//along with this program.  If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace TypeSwitchTest
{
    class TypeSwitchTest
    {
        static void Main(string[] args)
        {
            var methods =
                from m in typeof(TypeSwitchTest).GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                from a in m.GetCustomAttributes(false)
                let t = a as TimedTestAttribute
                where t != null
                orderby m.Name
                select (attr: t, method: m);

            // warm up the caches
            repeats = 10;
            foreach (var d in _testDataToSwitchOn)
            {
                object[] a = new[] { d };
                foreach (var (attr, method) in methods)
                {
                    var result = method.Invoke(null, a).ToString();
                }
            }

            // run the tests
            repeats = 20000000;
            foreach (var d in _testDataToSwitchOn)
            {
                object[] a = new[] { d };
                Console.WriteLine(d.GetType().Name);
                foreach (var (attr, method) in methods)
                {
                    Console.Write($" Test {method.Name:8}");
                    Stopwatch w = Stopwatch.StartNew();
                    var result = method.Invoke(null, a).ToString();
                    w.Stop();
                    Console.WriteLine($" Elapsed: {w.Elapsed} Result {result}");
                }
            }
        }

        static int repeats = 20000000;

        static readonly object[] _testDataToSwitchOn =
        {
            new C0(),
            //new C1(),
            //new C2(),
            //new C3(),
            //new C4(),
            new C5(),
            //new C6(),
            //new C7(),
            //new C8(),
            //new C9(),
            new C10(),
            new C11(),
            new C12(),
            new C13(),
            new C14(),
            new C15(),
            new C16(),
            new C17(),
            new C18(),
            new C19(),
            new C20(),
            //new C21(),
            //new C22(),
            //new C23(),
            //new C24(),
            new C25(),
            //new C26(),
            //new C27(),
            //new C28(),
            //new C29(),
            new C30(),
        };

        [TimedTest]
        static int T1_TypeSwitch(object d)
        {
                int sum = 0;
            for (int i = 0; i < repeats; i++)
            {
                sum += d switch
                {
                    C0 _ => 0,
                    C1 _ => 1,
                    C2 _ => 2,
                    C3 _ => 3,
                    C4 _ => 4,
                    C5 _ => 5,
                    C6 _ => 6,
                    C7 _ => 7,
                    C8 _ => 8,
                    C9 _ => 9,
                    C10 _ => 10,
                    C11 _ => 11,
                    C12 _ => 12,
                    C13 _ => 13,
                    C14 _ => 14,
                    C15 _ => 15,
                    C16 _ => 16,
                    C17 _ => 17,
                    C18 _ => 18,
                    C19 _ => 19,
                    C20 _ => 20,
                    C21 _ => 21,
                    C22 _ => 22,
                    C23 _ => 23,
                    C24 _ => 24,
                    C25 _ => 25,
                    C26 _ => 26,
                    C27 _ => 27,
                    C28 _ => 28,
                    C29 _ => 29,
                    C30 _ => 30,
                    _ => -1
                };
            }

            return sum;
        }

        [TimedTest]
        static int T2_LazyMap(object d)
        {
            int sum = 0;
            for (int i = 0; i < repeats; i++)
            {
                sum += TypeSwitchDispatch.GetIndex<(C0, C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16, C17, C18, C19, C20, C21, C22, C23, C24, C25, C26, C27, C28, C29, C30)>(d) switch
                {
                    0 => 0,
                    1 => 1,
                    2 => 2,
                    3 => 3,
                    4 => 4,
                    5 => 5,
                    6 => 6,
                    7 => 7,
                    8 => 8,
                    9 => 9,
                    10 => 10,
                    11 => 11,
                    12 => 12,
                    13 => 13,
                    14 => 14,
                    15 => 15,
                    16 => 16,
                    17 => 17,
                    18 => 18,
                    19 => 19,
                    20 => 20,
                    21 => 21,
                    22 => 22,
                    23 => 23,
                    24 => 24,
                    25 => 25,
                    26 => 26,
                    27 => 27,
                    28 => 28,
                    29 => 29,
                    30 => 30,
                    _ => -1,
                };
            }

            return sum;
        }
    }
}

public class TimedTestAttribute : Attribute
{
}

abstract class Base { }
class C0 : Base { }
class C1 : Base { }
class C2 : Base { }
class C3 : Base { }
class C4 : Base { }
class C5 : Base { }
class C6 : Base { }
class C7 : Base { }
class C8 : Base { }
class C9 : Base { }
class C10 : Base { }
class C11 : Base { }
class C12 : Base { }
class C13 : Base { }
class C14 : Base { }
class C15 : Base { }
class C16 : Base { }
class C17 : Base { }
class C18 : Base { }
class C19 : Base { }
class C20 : Base { }
class C21 : Base { }
class C22 : Base { }
class C23 : Base { }
class C24 : Base { }
class C25 : Base { }
class C26 : Base { }
class C27 : Base { }
class C28 : Base { }
class C29 : Base { }
class C30 : Base { }
