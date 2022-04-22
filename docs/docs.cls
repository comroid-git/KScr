\NeedsTeXFormat{LaTeX2e}
\ProvidesClass{docs}[3]
\LoadClass[11pt,a4paper,twoside]{article}
\usepackage{fancyhdr}
\usepackage{amsmath}
\usepackage[T1]{fontenc}
\usepackage{graphicx}
\usepackage[utf8]{inputenc}
\usepackage{listings}
\usepackage{url}
\usepackage{color}
\usepackage{tocloft}
\usepackage{makecell}
\usepackage{hyperref}
\usepackage{titlesec}
%%\usepackage{listings} does not work for some reason

\setcounter{secnumdepth}{4}

% -----------------------------------------------------------------------
% Page layout

% enlarge page
\setlength{\textwidth}{16cm}
\setlength{\topmargin}{-1cm}
\setlength{\evensidemargin}{0cm}
\setlength{\oddsidemargin}{0cm}
\setlength{\textheight}{24cm}

% skip between paragraphs
\setlength{\parskip}{1ex}
% ... and no indentation at start of a new paragraph
\setlength{\parindent}{0ex}

\pagestyle{fancy}
\thispagestyle{fancy}

% some colors similar to eclipse syntax highlighting
\definecolor{jred}{rgb}{0.6,0,0}
\definecolor{jgreen}{RGB}{63,127,95}
\definecolor{jpurple}{RGB}{127,0,85}
\definecolor{jblue}{RGB}{42,0,255}
\definecolor{jlightblue}{RGB}{63,95,191}
\definecolor{jgrey}{rgb}{0.46,0.45,0.48}

%Layout for lstlistings
\lstloadlanguages{Java} % Java-Sprache laden, notwendig wegen option 'savemem'
\lstset{
  language=Java,
  numbers=left,
  numberstyle=\tiny,
  numbersep=5pt,
  literate=%
    {Ö}{{\"O}}1
    {Ä}{{\"A}}1
    {Ü}{{\"U}}1
    {ß}{{\ss}}1
    {ü}{{\"u}}1
    {ä}{{\"a}}1
    {ö}{{\"o}}1
    {°}{{$^\circ$}}1,
  basicstyle=\ttfamily\scriptsize,
  showspaces=false,
  showtabs=false,
  showstringspaces=false,
  keywordstyle=\bfseries\color{jpurple},
  tabsize=2,
  frameround=ffff,
  extendedchars=true,
  commentstyle=\color{jgreen}\ttfamily,
  stringstyle=\color{jblue}\ttfamily,
  morecomment=[s][\color{jlightblue}]{/**}{*/},
  postbreak=\space,
  breakindent=5pt,
  breaklines=true
}

\endinput
