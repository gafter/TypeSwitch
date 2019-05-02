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

            TypeSwitchTest p = new TypeSwitchTest();
            foreach (var (attr, method) in methods)
            {
                Console.WriteLine($"Test {method.Name:8}");
                Stopwatch w = Stopwatch.StartNew();
                var result = method.Invoke(p, Array.Empty<object>()).ToString();
                w.Stop();
                Console.WriteLine($"  Elapsed: {w.Elapsed} Result {result}");
            }
        }

        const int repeats = 5000000;

        static readonly object[] _testDataToSwitchOn =
        {
            //new C40(),
            //new C39(),
            //new C38(),
            //new C37(),
            //new C36(),
            new C35(),
            new C34(),
            new C33(),
            new C32(),
            new C31(),
            new C30(),
            new C29(),
            new C28(),
            new C27(),
            new C26(),
            new C25(),
            new C24(),
            new C23(),
            new C22(),
            new C21(),
            new C20(),
            new C19(),
            new C18(),
            new C17(),
            new C16(),
            new C15(),
            new C14(),
            new C13(),
            new C12(),
            new C11(),
            new C10(),
            new C9(),
            new C8(),
            new C7(),
            new C6(),
            new C5(),
            new C4(),
            new C3(),
            new C2(),
            new C1(),
        };

        [TimedTest]
        static int T1_TypeSwitch()
        {
            int sum = 0;
            for (int i = 0; i < repeats; i++)
            {
                foreach (var d in _testDataToSwitchOn)
                {
                    sum += d switch
                    {
                        //C40 _ => 40,
                        //C39 _ => 39,
                        //C38 _ => 38,
                        //C37 _ => 37,
                        //C36 _ => 36,
                        //C35 _ => 35,
                        //C34 _ => 34,
                        //C33 _ => 33,
                        //C32 _ => 32,
                        //C31 _ => 31,
                        //C30 _ => 30,
                        //C29 _ => 29,
                        //C28 _ => 28,
                        //C27 _ => 27,
                        //C26 _ => 26,
                        //C25 _ => 25,
                        //C24 _ => 24,
                        //C23 _ => 23,
                        //C22 _ => 22,
                        //C21 _ => 21,
                        //C20 _ => 20,
                        //C19 _ => 19,
                        //C18 _ => 18,
                        //C17 _ => 17,
                        //C16 _ => 16,
                        //C15 _ => 15,
                        //C14 _ => 14,
                        //C13 _ => 13,
                        C12 _ => 12,
                        C11 _ => 11,
                        C10 x => 10,
                        C9 x => 9,
                        C8 x => 8,
                        C7 x => 7,
                        C6 x => 6,
                        C5 x => 5,
                        C4 x => 4,
                        C3 x => 3,
                        C2 x => 2,
                        C1 x => 1,
                        _ => -1
                    };
                }
            }

            return sum;
        }

        static readonly Type[] _typesOfTypeSwitch =
        {
            //typeof(C40),
            //typeof(C39),
            //typeof(C38),
            //typeof(C37),
            //typeof(C36),
            //typeof(C35),
            //typeof(C34),
            //typeof(C33),
            //typeof(C32),
            //typeof(C31),
            //typeof(C30),
            //typeof(C29),
            //typeof(C28),
            //typeof(C27),
            //typeof(C26),
            //typeof(C25),
            //typeof(C24),
            //typeof(C23),
            //typeof(C22),
            //typeof(C21),
            //typeof(C20),
            //typeof(C19),
            //typeof(C18),
            //typeof(C17),
            //typeof(C16),
            //typeof(C15),
            //typeof(C14),
            //typeof(C13),
            typeof(C12),
            typeof(C11),
            typeof(C10),
            typeof(C9),
            typeof(C8),
            typeof(C7),
            typeof(C6),
            typeof(C5),
            typeof(C4),
            typeof(C3),
            typeof(C2),
            typeof(C1),
        };
        static TypeSwitchDispatch _typeSwitchDispatch = new TypeSwitchDispatch(_typesOfTypeSwitch);
        static int _nTypesOfSwitch = _typesOfTypeSwitch.Length;

        [TimedTest]
        static int T2_LazyMap()
        {
            int sum = 0;
            for (int i = 0; i < repeats; i++)
            {
                foreach (var d in _testDataToSwitchOn)
                {
                    sum += _typeSwitchDispatch.GetIndex(d) switch
                    {
                        0 when d is C12 x => 12,
                        1 when d is C11 x => 11,
                        2 when d is C10 x => 10,
                        3 when d is C9 x => 9,
                        4 when d is C8 x => 8,
                        5 when d is C7 x => 7,
                        6 when d is C6 x => 6,
                        7 when d is C5 x => 5,
                        8 when d is C4 x => 4,
                        9 when d is C3 x => 3,
                        10 when d is C2 x => 2,
                        11 when d is C1 x => 1,
                        -1 => -1,
                        _ => throw null,
                    };
                }
            }

            return sum;
        }
    }
}

public class TimedTestAttribute : Attribute
{
}

abstract class Base { }
class C1 : Base { }
class C2 : C1 { }
class C3 : Base { }
class C4 : C3 { }
class C5 : Base { }
class C6 : Base { }
class C7 : C5 { }
class C8 : C7 { }
class C9 : C4 { }
class C10 : Base { }
class C11 : Base { }
class C12 : C5 { }
class C13 : Base { }
class C14 : Base { }
class C15 : C5 { }
class C16 : Base { }
class C17 : C9 { }
class C18 : C2 { }
class C19 : C18 { }
class C20 : Base { }
class C21 : Base { }
class C22 : Base { }
class C23 : Base { }
class C24 : Base { }
class C25 : Base { }
class C26 : C19 { }
class C27 : C5 { }
class C28 : C2 { }
class C29 : Base { }
class C30 : Base { }
class C31 : Base { }
class C32 : Base { }
class C33 : Base { }
class C34 : Base { }
class C35 : C19 { }
class C36 : C4 { }
class C37 : Base { }
class C38 : Base { }
class C39 : Base { }
class C40 : C33 { }

