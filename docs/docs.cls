\documentclass{docs}
\begin{document}
    \title{The KScr Language}
    \author{comroid}
    \begin{titlepage}
        \begin{center}
            \vspace*{1cm}
            
            \Huge
            \textbf{The KScr Language}
            
            \vspace{0.5cm}
            \LARGE
            Definition of the KScr Language
            
            \vspace{1.5cm}
            
            \textbf{comroid}
            
            \vspace{0.8cm}
            
            \includegraphics[width=0.4\textwidth]{img/kscr-icon.png}
            
            \vfill
            
            The KScr Language as defined by this document is a compiled high-level language whose goal is to provide runtime optimizations from a different approach.
            
            \vspace{1.2cm}
            
            \Large
            \underline{Team:} \\
            Kaleidox
        \end{center}
    \end{titlepage}
    \fancyhead[C]{{KScr Language}}
    \tableofcontents
    
    \pagebreak
    \section{Source Model}
    KScr expects source files to have the extension \texttt{.kscr}, and outputs binaries with the extension \texttt{.kbin}.
    \\
    All source files must start with a package declaration:
    \begin{verbatim}
        package org.comroid.kscr;
    \end{verbatim}
    Following the package declaration, an optional list of import statements follows; importing either canonical class paths or all static members of a class.
    
    \subsection{Modifiers}
    
    \subsubsection{Accessibility Modifiers}
    \begin{center}
    \begin{tabular}{c|c}
        Modifier & Function \\
        \hline
        \texttt{public} & Accessible from everywhere \\
        \texttt{internal} & Accessible during compilation; compiled to \texttt{private} \\
        \texttt{protected} & Accessible from inheritors \\
        \texttt{private} & Accessible from inside only
    \end{tabular}
    \end{center}
    
    \subsubsection{Other Modifiers}
    \begin{center}
    \begin{tabular}{c|c}
        Modifier & Function \\
        \hline
        \texttt{static} & Cannot be invoked dynamically \\
        \texttt{final} & Cannot be overridden or changed \\
        \texttt{abstract} & Must be implemented in inheritors \\
        \texttt{synchronized} & Invocations are synchronized \\
        \texttt{native} & Must be implemented by a native module
    \end{tabular}
    \end{center}
    
    \subsection{Classes}
    A class is declared by a class header of the following structure:
    \begin{verbatim}
        <accessibility modifiers> [class type] [class name]<type generics> <inherits> <body>
    \end{verbatim}
    Note that the class body can be optional when using a semicolon.
    
    \subsubsection{Class Types}
    A source class can be of different types:
    
    \paragraph{\texttt{class}-Type}
    A normal class that can be instantiated.
    Allowed modifiers are:
    \begin{center}
    \begin{tabular}{c|c}
        Modifier & Function \\
        \hline
        \texttt{static} & Cannot be instantiated and behaves like a Singleton \\
        \texttt{final} & Cannot be inherited by other classes \\
        \texttt{abstract} & Cannot be instantiated directly and must be inherited by other classes
    \end{tabular}
    \end{center}
    
    \paragraph{\texttt{enum}-Type}
    An enumeration of runtime-constants that follow a class-like pattern.
    This type does not allow modifiers.
    
    \paragraph{\texttt{interface}-Type}
    An interface that declares basic structure requirements for implementing classes.
    Cannot be instantiated directly.
    
    \paragraph{\texttt{annotation}-Type}
    A marker for most components of code. Used for flow control by enforcing rules at compile time, setting markers or carrying information at runtime.
    
    \subsubsection{Class Names}
    It is suggested that class names start with an upper case letter.
    
    \subsubsection{Type Generics}
    Type Generics are initially defined in a classes header, detailing the name with a postfix.
    They are surrounded by arrow-brackets \texttt{<\dots>}:
    \begin{verbatim}
        public class num<T> {}
    \end{verbatim}
    There is two special kinds of Type Generics;
    
    \label{typeGenN}
    \paragraph{\texttt{n}-Generic} The \texttt{n}-Generic serves as a type-based declaration for tuple types. \\
    Its usages must be integers, and their value is available at runtime in a semi-static field \texttt{public final int n}. \\
    Writing \texttt{string<2>} is the same as writing \texttt{tuple<2, string>}
    
    \label{typeGenList}
    \paragraph{Listing Generic} A listing Generic serves as a varargs-Generic. \\
    Its instance is an array of types which can be accessed at runtime in a semi-static field.
    
    \subsubsection{\texttt{extends} and \texttt{implements} inheritance setters}
    The \texttt{extends} and \texttt{implements} keywords follow the detailed class name definition and declare what classes or interfaces a class inherits. \\
    Both the \texttt{extends} and the \texttt{implements} listing can contain multiple members.
    
    \subsection{Class Members}
    
    \subsubsection{Static Initializer}
    The static initializer is declared by including a static member block in the class. \\
    It is executed after compile-time (or read-time if reading binary) and can modify the members of the containing class.
    \begin{verbatim}
        static {
            // initialize class right before late initialization
        }
    \end{verbatim}
    The static initializer is compile to be a method with the header \texttt{private static final void cctor()}
    
    \subsubsection{Constructors}
    The constructor is used to create a dynamic instance of the class. \\
    If a class \texttt{extends} more than one class, any of which does not support the default constructor, the constructor must declare all superconstructors like this:
    \begin{verbatim}
        public class Apple extends Fruit, Projectile implements Digestable {
            public Apple() : Fruit("Apple"), Projectile()
        }
    \end{verbatim}
    Constructors are compiled as Methods named \texttt{ctor}.
    
    \subsubsection{Methods}
    A method is a function of a class that can affect the class, compute a result or print "Hello, world!". \\
    Methods are distinguished from properties by their parameter definition, which may be empty. \\
    All methods must have an explicitly defined return type, but they may return \texttt{void}.
    
    \subsubsection{Properties}
    Properties are value computation access ports that can either hold a value, or compute it from a returning body. \\
    If such return bodies contain another property, then the other property is checked for its last update time. If it has been updated before the calling property has been, then computation is skipped and the last returned value is returned again.
    Setting a property updates it, causing all dependent properties to be computed again on their next access.
    
    \pagebreak
    \section{The KScr VM}
    
    \subsection{Built-in Types}

    \label{typeVoid}
    \subsubsection{\texttt{interface void}}
    The universal base type. Is implemented by all built-in types and can be implicitly cast to everything.

    \paragraph{Method \texttt{void\#toString(short alt)}}
    Used to obtain a string representation of the object.
    \\
    The \texttt{toString()} Method that is present in all objects has one optional parameter; a \texttt{short} that defines the alternative of the output string. Its default value is 0.
    \\
    The following values must be returned by different alternatives:
    \begin{center}
    \begin{tabular}{c|l}
        Value & Predefined output string \\
        \hline
        \texttt{0} & A parseable representation of the object \\
        \texttt{1} & A name that contains type information \\
        \texttt{2} & An undetailed name of the object \\
        \texttt{3} & A full undetailed name of the object \\
        \texttt{4} & A detailed name of the object \\
        \texttt{5} & A full detailed name of the object
    \end{tabular}
    \end{center}

    \paragraph{Method \texttt{void\#equals(void other)}}
    Used to test two objects for equality.
    This method is called by the \begin{math}=\end{math} and \begin{math}\neq\end{math} operators.

    \paragraph{Method \texttt{void\#getType()}}
    Used to obtain the exact class instance of an object.

    \paragraph{Property \texttt{void\#InternalID}}
    Used to obtain the internal ID of the object.
    
    \subsubsection{\texttt{class num<T>}}
    The base type of all numerics. Contains numeric subtypes:
    \begin{enumerate}
        \item \texttt{byte} - Type-alias for \texttt{int<8>} % todo: this is not true
        \item \texttt{short} - Type-alias for \texttt{int<16>}
        \item \texttt{int<n = 32>}
        \item \texttt{long} - Type-alias for \texttt{int<64>}
        \item \texttt{float}
        \item \texttt{double}
    \end{enumerate}
    All subtypes can be used directly, or using the Type Generic T, for example: \texttt{int<24> == num<int<24>>}
    
    \subsubsection{\texttt{class str}}
    The type of all strings.
    \\
    A string represents an array of characters.
    
    \subsubsection{\texttt{class object}}
    The base type of all non-built-in objects.
    \\
    Every foreign class that has no explicit \texttt{extends} definition implements this type implicitly.
    
    \subsubsection{\texttt{class array<T>}}
    The type of arrays. The type Generic \texttt{T} defines the type of the array.
    \\
    Writing \texttt{array<str>} is the same as writing \texttt{str[]}.
    
    \subsubsection{\texttt{class tuple<T...>}}
    The type of tuples. The type Generic \texttt{T...} defines all types of held values in order.
    \\
    If \texttt{T.Size == 1}, the tuple is of a singular type and might have been invoked by the \hyperref[typeGenN]{n-Type-Generic}; \texttt{T<n>}.
    Size must then be obtained by invoking \texttt{it.Size}.
    
    \subsubsection{\texttt{class enum<T>}}
    The type of enums. The type Generic \texttt{T} defines the output type of the enum.
    \\
    A class \texttt{enum Codes} would be of type \texttt{enum<Codes>} or simply \texttt{Codes}.
    
    \subsubsection{\texttt{class type<T>}}
    The type of all types. The type Generic \texttt{T} defines the actual type that is defined by this type.
    
    \subsubsection{\texttt{class pipe<T...>}}
    The type of pipes. The type Generic \texttt{T} defines the type of data that is handled by this pipe.
    
    \subsection{Literals}
    
    \subsubsection{\texttt{null}}
    The \texttt{null}-Literal. Is always of \hyperref[typeVoid]{type \texttt{void}}.
    
    \subsubsection{\texttt{Numeric Literals}}
    
    \subsubsection{\texttt{String Literals}}
    A string literal is pre- and superceded by a double-quote \texttt{"} symbol.
    An escaped double-quote \texttt{\textbackslash"} can be contained in the string.
    
    \paragraph{Planned Behaviour: Interpolation}
    A string supports interpolation using accolades with Formatter-support with the following syntax:
    \begin{verbatim}
        int hex = 1 << 3;
        stdio <<- "hex: {hex:X}"
        // prints "hex: 0x4"
    \end{verbatim}
    
    \subsubsection{\texttt{Array Literals}}
    Unimplemented.
    
    \subsubsection{\texttt{Tuple Literals}}
    Unimplemented.
    
    \subsubsection{\texttt{Other Literals}}
    
    \paragraph{\texttt{stdio}}
    Constantly represents the program's standard IO stream. \\
    The held value is of type \texttt{pipe<str>}

    \pagebreak
    \section{Core Features}

    \subsection{\texttt{Convert}-Class}

    \subsection{Property Caches}

    \subsection{Pipe Functionality}

    \pagebreak
    \section{KScr Build System}

    \subsection{Build Files}
\end{document}
