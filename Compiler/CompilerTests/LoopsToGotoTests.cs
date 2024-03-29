﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Atrufulgium.FrontTick.Compiler.Tests.TestHelpers;

namespace Atrufulgium.FrontTick.Compiler.Tests {
    [TestClass]
    public class LoopsToGotoTests {

        [TestMethod]
        public void WhileTest1()
            => TestCompilationSucceedsTheSame(@"
using MCMirror;
public class Test {
    public static void TestMethod(int i) {
    loop:
        if (i == 0) {
            i = 1;
            goto loop;
        }
    }
}
", @"
using MCMirror;
public class Test {
    public static void TestMethod(int i) {
        while (i == 0) {
            i = 1;
        }
    }
}
");

        [TestMethod]
        public void WhileTest2()
            => TestCompilationSucceedsTheSame(@"
using MCMirror;
public class Test {
    public static void TestMethod(int i) {
    loop:
        if (i == 0) {
            i = 1;
            goto loopend;
        }
    loopend: ;
    }
}
", @"
using MCMirror;
public class Test {
    public static void TestMethod(int i) {
        while (i == 0) {
            i = 1;
            break;
        }
    }
}
");

        [TestMethod]
        public void WhileTest3()
            => TestCompilationSucceedsTheSame(@"
using MCMirror;
public class Test {
    public static void TestMethod(int i) {
    loop:
        if (i == 0) {
            i = 1;
            if (i == 1)
                goto loopend;
            goto loop;
        }
    loopend: ;
    }
}
", @"
using MCMirror;
public class Test {
    public static void TestMethod(int i) {
        while (i == 0) {
            i = 1;
            if (i == 1)
                break;
        }
    }
}
");

        [TestMethod]
        public void WhileTest4()
            => TestCompilationSucceedsTheSame(@"
using MCMirror;
public class Test {
    public static void TestMethod(int i) {
    loop1:
        if (i == 0) {
            i = 1;
        loop2:
            if (i == 2) {
                i = 3;
                if (i == 4)
                    goto loop2break;
            loop3:
                if (i == 5) {
                    i = 6;
                    goto loop3;
                }
                goto loop2;
            }
        loop2break:
            goto loop1;
        }
    }
}
", @"
using MCMirror;
public class Test {
    public static void TestMethod(int i) {
        while (i == 0) {
            i = 1;
            while (i == 2) {
                i = 3;
                if (i == 4)
                    break;
                while (i == 5) {
                    i = 6;
                }
            }
        }
    }
}
");

        [TestMethod]
        public void WhileTest5()
    => TestCompilationSucceedsTheSame(@"
using MCMirror;
public class Test {
    public static void TestMethod(int i) {
    loop:
        if (i == 0) {
            i = 1;
            goto loop;
        }
    }
}
", @"
using MCMirror;
public class Test {
    public static void TestMethod(int i) {
        while (i == 0) {
            i = 1;
            continue;
        }
    }
}
");

        [TestMethod]
        public void WhileTest6()
    => TestCompilationSucceedsTheSame(@"
using MCMirror;
public class Test {
    public static void TestMethod(int i) {
    loop:
        if (i == 0) {
            i = 1;
            if (i == 2)
                goto loop;
            i = 3;
            goto loop;
        }
    }
}
", @"
using MCMirror;
public class Test {
    public static void TestMethod(int i) {
        while (i == 0) {
            i = 1;
            if (i == 2)
                continue;
            i = 3;
        }
    }
}
");

        [TestMethod]
        public void WhileTest7()
    => TestCompilationSucceedsTheSame(@"
using MCMirror;
public class Test {
    public static void TestMethod(int i) {
    loop:
        if (i == 0) {
            i = 1;
            if (i == 2)
                goto loop;
            else
                goto loopend;
        }
    loopend: ;
    }
}
", @"
using MCMirror;
public class Test {
    public static void TestMethod(int i) {
        while (i == 0) {
            i = 1;
            if (i == 2)
                continue;
            else
                break;
        }
    }
}
");

        [TestMethod]
        public void ForTest1()
            => TestCompilationSucceedsTheSame(@"
using MCMirror;
public class Test {
    public static void TestMethod(int i) {
        i = 1;
        while (i != 2) {
            i = 4;
            i += 3;
        }
    }
}
", @"
using MCMirror;
public class Test {
    public static void TestMethod(int i) {
        for (i = 1; i != 2; i += 3) {
            i = 4;
        }
    }
}
");

        [TestMethod]
        public void ForTest2()
            => TestCompilationSucceedsTheSame(@"
using MCMirror;
public class Test {
    public static void TestMethod(int i) {
        i = 1;
        while (i != 2) {
            i = 4;
            if (i == 5) {
                if (i == 6) {
                    i += 3;
                    continue;
                }
                i += 7;
                break;
            }
            i += 3;
        }
    }
}
", @"
using MCMirror;
public class Test {
    public static void TestMethod(int i) {
        for (i = 1; i != 2; i += 3) {
            i = 4;
            if (i == 5) {
                if (i == 6) {
                    continue;
                }
                i += 7;
                break;
            }
        }
    }
}
");

        [TestMethod]
        public void DoWhileTest1a()
            => TestCompilationSucceedsTheSame(@"
using MCMirror;
public class Test {
    public static void TestMethod(int i) {
    loopstart:
        i += 1;
        if (i != 2) goto loopstart;
        else goto loopend;
    loopend: ;
    }
}
", @"
using MCMirror;
public class Test {
    public static void TestMethod(int i) {
        do {
            i += 1;
        } while (i != 2);
    }
}
");

        [TestMethod]
        public void DoWhileTest1b()
            => TestCompilationSucceedsTheSame(@"
using MCMirror;
public class Test {
    public static void TestMethod(int i) {
        while (true) {
            i += 1;
            if (i != 2) continue;
            else break;
        }
    }
}
", @"
using MCMirror;
public class Test {
    public static void TestMethod(int i) {
        do {
            i += 1;
        } while (i != 2);
    }
}
");

        [TestMethod]
        public void DoWhileTest2a()
            => TestCompilationSucceedsTheSame(@"
using MCMirror;
public class Test {
    public static void TestMethod(int i) {
    loopstart:
        i += 1;
        if (i == 3) {
            if (i == 4)
                goto loopstart;
            goto loopend;
        }
        if (i != 2) goto loopstart;
        else goto loopend;
    loopend: ;
    }
}
", @"
using MCMirror;
public class Test {
    public static void TestMethod(int i) {
        do {
            i += 1;
            if (i == 3) {
                if (i == 4)
                    continue;
                break;
            }
        } while (i != 2);
    }
}
");

        [TestMethod]
        public void DoWhileTest2b()
            => TestCompilationSucceedsTheSame(@"
using MCMirror;
public class Test {
    public static void TestMethod(int i) {
        while (true) {
            i += 1;
            if (i == 3) {
                if (i == 4)
                    continue;
                break;
            }
            if (i != 2) continue;
            else break;
        }
    }
}
", @"
using MCMirror;
public class Test {
    public static void TestMethod(int i) {
        do {
            i += 1;
            if (i == 3) {
                if (i == 4)
                    continue;
                break;
            }
        } while (i != 2);
    }
}
");

        // Mixing for, while
        [TestMethod]
        public void MixedTest1()
            => TestCompilationSucceedsTheSame(@"
using MCMirror;
public class Test {
    public static void TestMethod(int i) {
        i = 1;
        while (i != 2) {
            while (i != 4) {
                i = 5;
                while (i != 6) {
                    while (i != 8) {
                        if (i == 9)
                            continue;
                    }
                    if (i == 10) {
                        i += 7;
                        continue;
                    }
                    i += 7;
                }
                if (i == 11)
                    continue;
            }
            if (i == 12) {
                i += 3;
                continue;
            }
            i += 3;
        }
    }
}
", @"
using MCMirror;
public class Test {
    public static void TestMethod(int i) {
        for (i = 1; i != 2; i += 3) {
            while (i != 4) {
                for (i = 5; i != 6; i += 7) {
                    while (i != 8) {
                        if (i == 9)
                            continue;
                    }
                    if (i == 10)
                        continue;
                }
                if (i == 11)
                    continue;
            }
            if (i == 12)
                continue;
        }
    }
}
");

        // Mixing for, do while
        [TestMethod]
        public void MixedTest2()
            => TestCompilationSucceedsTheSame(@"
using MCMirror;
public class Test {
    public static void TestMethod(int i) {
        i = 1;
        while (i != 2) {
            while (true) {
                i = 5;
                while (i != 6) {
                    while (true) {
                        if (i == 9)
                            continue;
                        if (i != 8) continue;
                        else break;
                    }
                    if (i == 10) {
                        i += 7;
                        continue;
                    }
                    i += 7;
                }
                if (i == 11)
                    continue;
                if (i != 4) continue;
                else break;
            }
            if (i == 12) {
                i += 3;
                continue;
            }
            i += 3;
        }
    }
}
", @"
using MCMirror;
public class Test {
    public static void TestMethod(int i) {
        for (i = 1; i != 2; i += 3) {
            do {
                for (i = 5; i != 6; i += 7) {
                    do {
                        if (i == 9)
                            continue;
                    } while (i != 8);
                    if (i == 10)
                        continue;
                }
                if (i == 11)
                    continue;
            } while (i != 4);
            if (i == 12)
                continue;
        }
    }
}
");
    }
}