#! /usr/bin/env gforth

\ autohipnosis.fs

\ This is the main file of «Autohipnosis»,
\ an experimental interactive fiction in Spanish.

\ Version 0.3.0+201708182027

\ http://programandala.net/es.programa.autohipnosis

\ }}} ==========================================================
\ Author and License {{{

\ Author: Marcos Cruz (programandala.net), 2012, 2015, 2016, 2017.

\ You may do whatever you want with this work, so long as you retain
\ the copyright/authorship/acknowledgment/credit notice(s) and this
\ license in all redistributed copies and derived works.  There is no
\ warranty.

\ }}} ==========================================================

\ This program is written in Forth for the Gforth system:
\ <http://gnu.org/software/gforth>

\ Development history (in Spanish):
\ <http://programandala.net/es.programa.autohipnosis.historial.html>

\ Information on text based games (in Spanish):
\ <http://caad.es>

\ }}} ==========================================================
\ Stack notation  {{{

\ XXX TODO

\ }}} ==========================================================
\ Requirements {{{

s" PWD" getenv fpath also-path

\ ----------------------------------------------
\ From Forth Foundation Library
\ (http://irdvo.github.io/ffl/)

require ffl/trm.fs
\ ----------------------------------------------
\ From the Galope library
\ (http://programandala.net/en.program.galope.html)

require galope/at-x.fs                    \ 'at-x'
require galope/column.fs                  \ 'column'
require galope/fifty-percent-nullify.fs   \ `50%nullify`
require galope/print.fs                   \ justified print
require galope/s-curly-bracket.fs         \ `s{`, `}s`.
require galope/s-plus.fs                  \ 's+'
require galope/randomize.fs               \ 'randomize'
require galope/row.fs                     \ 'row'
require galope/sb.fs                      \ circular string buffer
require galope/seconds.fs                 \ 'seconds'
require galope/two-drops.fs               \ '2drops'
require galope/xy.fs                      \ 'xy'

\ ' bs+ alias s+
' bs& alias s&
\ ' bs" alias s" immediate
1024 heap_sb

\ }}} ==========================================================
\ Stock words {{{

: wait  ( -- )  key drop  ;
: show  ( -- )  cr .s  ;
: show...  ( -- )  show wait  ;

 ' true alias [true]   immediate
' false alias [false]  immediate

: period+  ( ca1 len1 -- ca2 len2 )  s" ." s+  ;
  \ Add a full stop at the end of string _ca1 len1_.

: comma+  ( ca1 len1 -- ca2 len2 )  s" ," s+  ;
  \ Add a comma at the end of string _ca1 len1_.

\ }}} ==========================================================
\ Vocabularies {{{

  \ XXX TODO use word lists instead

vocabulary game-vocabulary      \ words of the game
vocabulary player-vocabulary    \ words usable by the player

\ vocabulary answer-vocabulary  \ answers to yes/no questions
  \ XXX TODO -- not used yet

vocabulary menu-vocabulary      \ words of menu options

: restore-vocabularies  ( -- )
  only forth also game-vocabulary definitions  ;
  \ Restore the normal search order.

restore-vocabularies

\ }}} ==========================================================
\ Screen {{{

: last-col  ( -- u )  cols 1-  ;
: last-row  ( -- u )  rows 1-  ;

false  [if]
  \ XXX INFORMER:
  cr ." rows x cols = " rows . cols .
  cr ." last row = " last-row .
  cr ." last col = " last-col .
  wait
[then]

: no-window  ( -- )
  [char] r trm+do-csi0  ;
  \ Deactivate the screen zone as a window. 

: output-window  ( -- )
  last-row 1 - 1 trm+set-scroll-region  ;
  \ Select a screen zone (all rows except two at the bottom) as main output window.
  \ Note: 
  \ For 'trm+set-scroll-region' first row is 1, while for Forth-94 it's 0.

: at-first-output  ( -- )
  0 last-row 2 - at-xy  ;
  \ Set the cursor position where the first sentence will be printed
  \ (at the bottom of the screen).

: init-output-cursor  ( -- )
  output-window
  at-first-output trm+save-current-state
  no-window  ;

: at-input  ( -- )
  0 last-row at-xy  ;
  \ Set the cursor position at the input zone (last row).
  \ XXX OLD -- not used

\ }}} ==========================================================
\ Text output {{{

2 value /indentation
  \ Indentation of the first line of every paragraph.

variable indent-first-line-too?
  \ Flag: Do indent also the first row?

: not-first-line?  ( -- wf )
  row 0>  ;
  \ Is the cursor not at the first row?

: indentation?  ( -- wf )
  not-first-line? indent-first-line-too? @ or  ;
  \ Do indent the current row?

: (indent)  ( -- )
  /indentation print_indentation  ;
  \ Indent.

: indent  ( -- )
  indentation? if  (indent)  then  ;
  \ Indent if needed.

: cr+  ( -- )
  print_cr indent   ;

: paragraph  ( ca len -- )
  cr+ print  ;
  \ Print the paragraph _ca len_ justified.

\ }}} ==========================================================
\ Title {{{

: title-row  ( col -- )
  cr at-x  ;
  \ Do a carriage return and put the cursor at column _col_.

: .title  ( -- )
  column
  ."   _" dup title-row
  ."  /_)    _)_ _ ( _  o  _   _   _   _ o  _" dup title-row
  ." / / (_( (_ (_) ) ) ( )_) ) ) (_) (  ( ("  dup title-row
  ."                     (            _)   _)" title-row  ;
  \ Print the main title at the current cursor position.

40 constant title-width
4 constant title-height

: margin  ( u1 u2 -- u3 )  - 2/  ;
  \ Margin needed to center a text of _u1_ characters or rows on on a
  \ screen of _u2_ columns or rows.

: .centered-title  ( -- )
  cols title-width margin
  rows title-height margin
  at-xy .title  ;
  \ Print the title centered on the screen.

: len>col  ( len -- col )  cols swap margin  ;
  \ Column needed to center a text of _len_ characters.

: center-type  ( ca len -- )
  dup len>col at-x type  ;

: greeting  ( -- )
  page
  s" Pulsa una tecla para empezar" center-type
  .centered-title  ;

\ }}} ==========================================================
\ Data {{{

variable #sentences  0 #sentences !
  \ number of sentences

defer 'sentences
  \ table to hold the addresses of the sentences

: >sentence>  ( u1 -- u2 )
  #sentences @ swap -  ;
  \ Convert the sentence ordinal number _u1_
  \ to its item number _u2_ in the sentences table.

: 'sentence  ( u -- ca )
  >sentence> cells 'sentences + @  ;
  \ Convert the sentence number _u_ to the sentence address _ca_.

: sentence$  ( u -- ca len )
  'sentence count  ;
  \ Return the sentence _ca len_ whose ordinal number is _u_.

: .sentence  ( u -- )
  output-window trm+restore-current-state
  sentence$ paragraph trm+save-current-state
  no-window  ;
  \ Print the sentence whose ordinal number is _u_.

: hs,  ( ca len -- ca1 )
  here rot rot s,  ;
  \ Compile a string _ca len_ and return its address _ca1_.

: sentence:  ( ca len "name" -- ca1 )
  hs,  #sentences 1 over +!  @ constant  ;
  \ Compile a sentence _ca len_, returning its address _ca1_;
  \ create a constant "name" that holds the sentence id (its
  \ ordinal number).

include autohipnosis_sentences.fs
  \ Compile the sentences.

\ At this point, all sentences have been compiled and their addresses
\ are on the stack. The variable `sentences#` holds the count.

: sentences,  ( a1 ... an -- )
  #sentences @ 0  ?do  ,  loop  ;
  \ Compile the addresses of the sentences.
  \ XXX FIXME -- check the colon-sys!

create ('sentences)  ' ('sentences) is 'sentences
  \ table to hold the addresses of the sentences

sentences,  \ compile the data

: associated?  ( u a | a u -- wf )
  + c@ 0<>  ;
  \ Is a term whose body is _a_ associated to a sentence id _u_?

: associate  ( u a -- )
  + true swap c!  ;
  \ Associate a sentence id _u_ to a term whose body is _a_.

: execute-nt  ( i*x nt -- j*x )
  name>int execute  ;
  \ Execute the interpretation semantics of _nt_.

: execute-latest  ( i*x -- j*x )
  latest execute-nt  ;
  \ Execute the interpretation behaviour of the most recent word
  \ defined.

: update-term  ( u nt -- )
  execute-nt  ( u a )  associate  ;
  \ Associate a term _nt_ to a sentence _u_.

' variable alias term
  \ This alias is needed because the `forth` vocabulary will not
  \ be visible while terms are being created.

\ ' create alias variablx \ XXX try
\ XXX FIXME using 'create' instead of 'variable' causes an error

: create-term-word  ( ca len -- )
  nextname term  ;
  \ Create a word for term _ca len_.

: create-term-array  ( -- )
  here #sentences @ dup allot align erase  ;
  \ Create the array that holds the sentence ids a term is
  \ associated to: one byte per every possible sentence.

: create-term  ( ca len -- )
  create-term-word create-term-array  ;
  \ Create a term _ca len_, which will work as a variable but with a
  \ larger body: one byte for every defined sentence.

: init-term  ( u -- )
  execute-latest associate  ;
  \ Init the most recently created term, associating it to sentence
  \ _u_.

: new-term  ( u ca len -- )
  create-term init-term  ;
  \ Create a new term _ca len_ and associate it to sentence _u_.

: another-term  ( u ca len -- )
  2dup find-name  ?dup
  if  nip nip update-term  else  new-term  then  ;
  \ Create or update a term _ca len_ associated to a sentence _u_.

: parse-term  ( "name" -- ca len )
  begin   parse-name dup 0=
  while   2drop refill 0= abort" Error en el código fuente: falta un '}terms'"
  repeat  ;
  \ Parse the next term.

: another-term?  ( "name" -- ca len f )
  parse-term 2dup s" }terms" compare  ;
  \ Is there another term in the list?  Parse a word and check if it's
  \ the last term associated to a sentence. Return the parsed word _ca
  \ len_; _f_ is true if it's not te end of the list.

: terms{  ( u "name#0" ... "name#n" "}terms" -- )
  only game-vocabulary also player-vocabulary definitions
\  assert( depth 1 = ) \ XXX INFORMER
  begin
    dup another-term? ( u u a1 u1 f )
\    assert( depth 5 = ) \ XXX INFORMER
  while   another-term
\    assert( depth 1 = ) \ XXX INFORMER
  repeat
\  assert( depth 4 = ) \ XXX INFORMER
  2drop 2drop
  restore-vocabularies  ;
  \ Create or update words associated to a sentence _u_.

include autohipnosis_terms.fs
  \ Create the terms.

\ Define the special game commands:

also player-vocabulary definitions

: #fin ( -- )
  \ XXX TODO
  ;

restore-vocabularies

\ }}} ==========================================================
\ Command interpreter {{{

variable sentence#  \ current sentence
variable valid      \ counter: valid terms per command

: valid++  (  a -- )
  sentence# @ associated? abs valid +!  ;
  \ Update the score. _a_ is the body of a term associated to a
  \ sentence.

: execute-term  ( nt -- )
  execute-nt  ( a ) valid++  ;
  \ Execute a term _nt_ associated to a sentence.

: (evaluate-command)  ( -- )
  begin   parse-name ?dup
  while   find-name ?dup if  execute-term  then
  repeat  drop  ;
  \ Parse the source with the current search order:
  \ words recognized will be executed as terms associated to a
  \ sentence.

: evaluate-command  ( ca len -- )
  only player-vocabulary
  ['] (evaluate-command) execute-parsing
  restore-vocabularies  ;
  \ Evaluate the string _ca len_ using the player word list.

variable testing

: valid?  ( ca len -- wf )
  valid off  evaluate-command  valid @ 0<>
  testing @ or  ;
  \ Does the string _ca len_ contain a term associated to the
  \ current sentence?

: prompt$  ( -- ca len )
  s" > "  ;

: /command  ( -- u )
  cols prompt$ nip - 1-  ;
  \ Maximum length of a command.

: init-command-line  ( -- )
  0 last-row at-xy trm+erase-line  ;
  \ Clear the command line and set the cursor position.

create 'command /command chars allot align

: (command)  ( -- ca len )
  'command /command accept  'command swap  ;
  \ Accept a player command.

: .prompt  ( -- )
  prompt$ type  ;

: command  ( -- ca len )
  init-command-line .prompt (command)  ;
  \ Init the screen and accept a player command.

\ }}} ==========================================================
\ The end {{{

variable success?  \ flag

: happy-end  ( -- )
  success? on  ;
  \ XXX TODO

\ }}} ==========================================================
\ Help {{{

: curiosities  ( -- )
  s" Curiosidades..." paragraph  ;
  \ XXX TODO

: game$  ( -- ca len )
  \ s{ s" juego" s" programa" }s  ;  \ XXX OLD
  s" programa"  ;

: the-game$  ( -- ca len )
  s" el" game$ s&  ;

: except$  ( -- ca len )
  s{ s" excepto" s" salvo" }s  ;

: way$  ( -- ca len )
  s{ s" manera" s" forma" }s  ;

: leave$  ( -- ca len )
  s{ s" abandonar" s" detener" s" dejar" s" interrumpir" }s  ;

: left$  ( -- ca len )
  s{ s" abandonado" s" detenido" s" dejado" s" interrumpido" }s  ;

: pressing$  ( -- ca len )
  s{ s" pulsando" s" mediante" }s  ;

: they-make$  ( -- ca len )
  s{ s" forman" s" componen" }s  ;

: instructions-0  ( -- )
  s{ s" El" s" Este" }s game$ s&
  s{ s" mostrará" s" imprimirá" }s bs&
  s" un texto" s& s" en la pantalla" 50%nullify bs&
  s" y" s&
  s{ s" a continuación" s" después" s" seguidamente" }s 50%nullify bs&
  s{ s" esperará" s" se quedará esperando" }s bs&  s" una respuesta." s&
  s{
    \ s" El" s{ s" juego" s" objetivo" }s bs& s" consiste en" s& \ XXX OLD
    s" El objetivo consiste en"
    s" Lo que" s{ s" has de" s" tienes que" s" hay que" s" debes" }s bs& s" hacer es" s&
    s" El jugador" s{ s" debe" s" tiene que" }s bs&
    s{ s" Tienes que" s" Debes" s" Has de" s" Hay que" }s
    s" Tu" s{ s" objetivo" s" misión" }s bs& s{ s" es" s" será" s" consiste en" }s bs&
  }s bs&
  s{ s" responder a" s" escribir una respuesta para" }s bs&
  s" cada texto," s&
  s{ s" usando" s" empleando" s" utilizando" s" incluyendo" }s bs&
  s{ s" como mínimo" s" al menos" s" por lo menos" }s bs&
  s" un sustantivo" s& s" (en singular)" 50%nullify bs&
  s{ s" relacionado" s" que tenga relación" }s bs& s" con" s&
  s{ s" el mismo" s" él" }s bs& comma+
  s" pero que no sea" s&
  s{ s" familia de" s" de la misma familia que" }s bs&
  s" alguna de las palabras" s&
  s{
  s" que lo" they-make$ s&
  s" que" they-make$ bs& s{ s" el" s" dicho" }s bs& s" texto" s&
  s{ s" del" s" de dicho" }s s" texto" s&
  }s bs& period+
  s" El proceso" s&
  s{  s" se repetirá" s" continuará"
      s" no acabará" s" no terminará"
      s" seguirá" s" durará"
  }s bs&
  s" hasta que todos los textos hayan sido mostrados y respondidos." s&
  paragraph  ;
  \ Instructions on the game goal.

: instructions-1  ( -- )
  s{
  s" No es posible" leave$ s& the-game$ s& comma+ except$ s& pressing$ s&
  s" El" game$ s& s" no puede ser" s& left$ s& comma+ except$ s& pressing$ s&
  s" La única" way$ s& s" de" s& leave$ s& the-game$ s& s" es pulsar" s&
  }s
  s{ s" la combinación de teclas" s" el atajo de teclado" s" las teclas" }s bs&
  s" «Ctrl»+«C»," s&
  s{ s" lo que" s" lo cual" }s bs& s" te" s&
  s{ s" devolverá" s" hará regresar" }s bs&
  s{ s" a la línea de comandos" s" al intérprete" }s bs&
  s" de Forth." s&
  paragraph  ;
  \ Instructions on leaving the game.
  \ XXX FIXME -- Ctrl+C return to the OS shell

: instructions-2  ( -- )
  s" Tanto para empezar a jugar ahora como para hacerlo tras haber"
  left$ s& the-game$ s&
  s" puedes" s& s{ s" usar" s" probar" }s bs&
  s" cualquier palabra que" s&
  s{ s" se te ocurra" s" te parezca" }s bs& comma+ s" hasta" s&
  s{ s" encontrar" s" dar con" s" acertar con" }s bs&
  s{ s" alguna" s" una" }s bs& s" que" s&
  s{ s" surta efecto" s" funcione" s" sirva" }s bs& period+
  paragraph  ;
  \ Intructions on the game start.

: instructions  ( -- false )
  \ page s" Instrucciones de Autohipnosis" paragraph cr cr
  page instructions-0 instructions-1 instructions-2  false  ;
  \ XXX TODO

\ }}} ==========================================================
\ Menu {{{

: menu-0$  ( -- ca len )
  s" Qué"
  s{ s{ s" quieres" s" deseas" }s s" hacer" 50%nullify bs&
  s" ordenas" }s bs&  ;
  \ First version of the menu text.

: menu-1$  ( -- ca len )
  s" Cuáles son tus"
  s{ s" órdenes" s" instrucciones" }s bs&  ;
  \ Second version of the menu text.

: menu$  ( -- ca len )
  s" ¿" s{ menu-0$ menu-1$ }s bs+ s" ?" s+  ;
  \ Menu text.

: .menu  ( -- )
  menu$ key? drop paragraph  ;
  \ Print the menu.
  \ XXX FIXME -- Somehow `key? drop` prevents some control chars
  \ from appearing.

: (evaluate-option)  ( -- )
  begin   parse-name ?dup
  while   find-name ?dup   if  execute-nt  then
  repeat  drop  ;
  \ Parse the current source, executing only the recognized
  \ words.

: evaluate-option  ( ca len -- )
  only menu-vocabulary
  ['] (evaluate-option) execute-parsing
  restore-vocabularies  ;
  \ Evaluate a menu option _ca len_.

variable finished  \ flag: quit the program?

: finish  ( -- )
  finished on  ;

: menu  ( -- wf )
  finished off  .menu command evaluate-option finished @  ;
  \ Show the menu and execute an option. Return _true_
  \ if the player wants to quit.

\ }}} ==========================================================
\ Init {{{

: init-once  ( -- )
  page greeting 20 seconds  instructions drop  ;
  \ Init needed only once.

: init-game  ( -- )
  init-output-cursor  page  ;
  \ Init needed before every game.

\ }}} ==========================================================
\ Game {{{

: ask  ( u -- )
  sentence# !  begin  command  valid?  until  ;
  \ Ask the player for a command for the sentence number _u_,
  \ until a valid command is receceived.

: step  ( u -- )
  dup .sentence ask  ;
  \ One step of the game. _u_ is the number of the current sentence.

: game  ( -- )
  #sentences @ dup 1
  do  i step  loop
  .sentence  \ final sentence
  happy-end  ;

: play  ( -- )
  init-game game   ;

\ }}} ==========================================================
\ Main {{{

: farewell$  ( -- ca len )
  s{  s" Adiós"
      s" Hasta" s{ s" otra" s" la vista" s" pronto" s" luego" s" más ver" }s bs&
  }s  period+  ;

: farewell  ( -- )
  page farewell$ paragraph space 2 seconds bye  ;

: main ( -- )
  init-once
  begin  menu  until  farewell  ;
  \ Main loop.

\ }}} ==========================================================
\ Menu commands {{{

\ There are only three menu commands, with many synonyms.

also menu-vocabulary definitions

\ Synonyms for the `instructions` command:

' instructions alias aclaración
' instructions alias aclaraciÓn
' instructions alias ayuda
' instructions alias ex
' instructions alias examina
' instructions alias examinad
' instructions alias examinar
' instructions alias examino
' instructions alias examínate
' instructions alias examÍnate
' instructions alias examínome
' instructions alias examÍnome
' instructions alias explicación
' instructions alias explicaciÓn
' instructions alias guía
' instructions alias guÍa
' instructions alias i
' instructions alias instrucciones
' instructions alias inventario
' instructions alias lee
' instructions alias leed
' instructions alias leer
' instructions alias leo
' instructions alias m
' instructions alias manual
' instructions alias mira
' instructions alias mirad
' instructions alias mirar
' instructions alias miro
' instructions alias mírate
' instructions alias mÍrate
' instructions alias mírome
' instructions alias mÍrome
' instructions alias pista
' instructions alias pistas
' instructions alias registra
' instructions alias registrad
' instructions alias registrar
' instructions alias registro
' instructions alias salida
' instructions alias salidas
' instructions alias tutorial
' instructions alias x

\ Synonyms for the `play` command:

' play alias abajo
' play alias adelante
' play alias arranca
' play alias arrancad
' play alias arrancar
' play alias arranco
' play alias arriba
' play alias baja
' play alias bajad
' play alias bajar
' play alias bajo
' play alias comencemos
' play alias comenzad
' play alias comenzar
' play alias comienza
' play alias comienzo
' play alias e
' play alias ejecuta
' play alias ejecutad
' play alias ejecutar
' play alias ejecuto
' play alias ejecútate
' play alias empecemos
' play alias empezad
' play alias empezar
' play alias empieza
' play alias empiezo
' play alias entra
' play alias entrad
' play alias entrar
' play alias entremos
' play alias entro
' play alias este
' play alias inicia
' play alias iniciad
' play alias iniciar
' play alias iniciemos
' play alias inicio
' play alias iníciate
' play alias juega
' play alias juego
' play alias jugad
' play alias jugar
' play alias juguemos
' play alias n
' play alias ne
' play alias no
' play alias noreste
' play alias noroeste
' play alias norte
' play alias o
' play alias oeste
' play alias partida
' play alias probad
' play alias probar
' play alias probemos
' play alias prueba
' play alias pruebo
' play alias s
' play alias se
' play alias so
' play alias sube
' play alias subid
' play alias subir
' play alias subo
' play alias sudeste
' play alias sudoeste
' play alias sur
' play alias sureste
' play alias suroeste
' play alias venga

\ Synonyms for the `finish` command:

' finish alias acaba
' finish alias acabad
' finish alias acabar
' finish alias acabo
' finish alias acabose
' finish alias acabó
' finish alias acabÓ
' finish alias adiós
' finish alias adiÓs
' finish alias apaga
' finish alias apagad
' finish alias apagar
' finish alias apago
' finish alias apagón
' finish alias apagÓn
' finish alias apágate
' finish alias apÁgate
' finish alias cerrad
' finish alias cerrar
' finish alias cierra
' finish alias cierre
' finish alias cierro
' finish alias ciérrate
' finish alias ciÉrrate
' finish alias concluid
' finish alias concluir
' finish alias conclusión
' finish alias conclusiÓn
' finish alias concluye
' finish alias concluyo
' finish alias desconecta
' finish alias desconectad
' finish alias desconectar
' finish alias desconecto
' finish alias desconexión
' finish alias desconexiÓn
' finish alias desconéctate
' finish alias desconÉctate
' finish alias fin
' finish alias final
' finish alias finaliza
' finish alias finalización
' finish alias finalizaciÓn
' finish alias finalizad
' finish alias finalizar
' finish alias finalizo
' finish alias sal
' finish alias salgo
' finish alias salid
\ ' finish alias salida
' finish alias salir
' finish alias termina
' finish alias terminación
' finish alias terminaciÓn
' finish alias terminad
' finish alias terminar
' finish alias termino
' finish alias término
' finish alias tÉrmino

restore-vocabularies

\ }}} ==========================================================
\ Debug tools {{{

true [if]

: .sentences  ( a -- )
  cr #sentences @ 0 ?do
    dup i + c@ if  i sentence$ type cr  then
  loop  drop  ;
  \ Print all sentences associated to term hold in address _a_.
  \ Usage examples:
  \   also player-vocabulary
  \   confort .sentences

\ Build a fake term "z", valid for all sentences:

also player-vocabulary definitions
0 s" z" new-term
z #sentences @ true fill
restore-vocabularies

[then]

\ }}} ==========================================================
\ Boot

' noop is dobacktrace
  \ Don't show the return stack backtrace after an error.

' main alias autohipnosis
  \ Just to force "autohipnosis" to be shown after a break.

autohipnosis

\ vim: filetype=gforth foldmethod=marker
