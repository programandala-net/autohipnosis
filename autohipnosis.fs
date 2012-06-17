\ autohipnosis.fs

\ Main file of
\ «Autohipnosis» (version A-00-2012061723),
\ an experimental text game in Spanish.

\ http://programandala.net/es.programa.autohipnosis

\ Copyright (C) 2012 Marcos Cruz (programandala.net)

\ Autohipnosis is free software; you can redistribute it
\ and/or modify it under the terms of the GNU General Public
\ License as published by the Free Software Foundation;
\ either version 2 of the License, or (at your option) any
\ later version.

\ Autohipnosis is distributed in the hope that it will be
\ useful, but WITHOUT ANY WARRANTY; without even the implied
\ warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR
\ PURPOSE.  See the GNU General Public License for more
\ details.

\ You should have received a copy of the GNU General Public
\ License along with this program; if not, see
\ http://www.gnu.org/licenses .

\ }}} ##########################################################

\ Este programa está escrito en Forth usando el sistema Gforth:
\ http://www.jwdt.com/~paysan/gforth.html
\ Para escribir este programa se ha empleado el editor Vim:
\ http://www.vim.org

\ Historial de desarrollo:
\ http://programandala.net/es.programa.autohipnosis.historial

\ Información sobre juegos conversacionales:
\ http://caad.es
 
\ }}} ##########################################################
\ Notación de la pila {{{

(

En este programa usamos las siguientes abreviaturas para
describir los elementos de la pila:

+n    = número de 32 bitios positivo
-n    = número de 32 bitios negativo
...   = elipsis: número variable de elementos, o rango
a     = dirección de memoria
a u   = dirección y longitud de zona de memoria, p.e. de un texto
b     = octeto, valor de ocho bitios
c     = carácter de un octeto
f     = indicador lógico: cero significa «falso»; otro valor significa «cierto»
false = 0
ff    = indicador puro de Forth:
        0=«falso»; -1=«cierto»
        [-1 es un valor de 32 bitios con todos los bitios a uno]
i*x   = grupo de elementos sin especificar; puede estar vacío
j*x   = grupo de elementos sin especificar; puede estar vacío
n     = número de 32 bitios con signo
nt    = identificador de nombre de una palabra, notación de Gforth.
true  = -1 [valor de 32 bitios con todos los bitios a uno]
u     = número de 32 bitios sin signo
x     = valor sin determinar 32 bitios
xt    = identificador de ejecución de una palabra,
        notación de ANS Forth análoga a «cfa» en Forth clásico

Como es costumbre, los diferentes elementos del mismo tipo
se distinguirán con un sufijo, casi siempre un dígito,
o bien un apóstrofo, según los casos.

)

\ }}} ##########################################################
\ Requisitos {{{

\ -----------------------------
\ De «Forth Foundation Library» (versión 0.8.0)
\ (http://code.google.com/p/ffl/)

\ Manejador de secuencias de escape de la consola:
s" ffl/trm.fs" required

\ -----------------------------
\ De programandala.net

require ghoul/sb.fs  \ Almacén circular de textos
\ ' bs+ alias s+
' bs& alias s&
\ ' bs" alias s" immediate
1024 heap_sb

require ghoul/random_strings.fs  \ Textos aleatorios
require ghoul/xy.fs  \ 'xy'
require ghoul/row.fs  \ 'row'
require ghoul/column.fs  \ 'column'
require ghoul/at-x.fs  \ 'at-x'
require ghoul/print.fs  \ Impresión de textos justificados
require ghoul/randomize.fs  \ 'randomize'

\ }}} ##########################################################
\ Herramientas {{{

: show  cr .s  ;
: wait  key drop  ;
: show...  show wait  ;
' true alias [true]  immediate
' false alias [false]  immediate
: period+  ( a1 u1 -- a2 u2 )
  \ Añade un punto al final de una cadena.
  s" ." s+  ;
: comma+  ( a1 u1 -- a2 u2 )
  \ Añade una coma al final de una cadena.
  s" ," s+  ;

\ }}} ##########################################################
\ Vocabularios {{{

vocabulary game_vocabulary  \ palabras del programa
vocabulary player_vocabulary  \ palabras del jugador
\ No se usa todavía!!!:
\ vocabulary answer_vocabulary  \ respuestas a preguntas de «sí» o «no»
vocabulary menu_vocabulary  \ palabras para las opciones del menú

: restore_vocabularies
  \ Restaura los vocabularios a su orden habitual.
  only forth also game_vocabulary definitions  ;
restore_vocabularies

\ }}} ##########################################################
\ Pantalla {{{

: last_col  ( -- u )  cols 1-  ;
: last_row  ( -- u )  rows 1-  ;

false  [if]
\ Debug!!!:
cr ." rows x cols = " rows . cols .
cr ." last row = " last_row . 
cr ." last col = " last_col .
wait
[then]

: no_window
  \ Desactiva la definición de zona de pantalla como ventana.
  [char] r trm+do-csi0  ;

: output_window
  \ Selecciona una zona de pantalla para la salida principal
  \ (todas las líneas salvo las dos últimas).
  \ Nótese que TRM+SET-SCROLL-REGION cuenta las líneas empezando por uno,
  \ mientras que ANS Forth cuenta líneas y columnas empezando por cero.
  last_row 1- 1 trm+set-scroll-region
  \ last_row 2 - 1 trm+set-scroll-region  ;
  \ 10 1 trm+set-scroll-region 
  ;
2variable output-xy  \ Coordenadas del cursor en la ventana de salida
: save_output_cursor
  \ Guarda la posición actual del cursor en la ventana de salida.
  xy output-xy 2!  ;
: restore_output_cursor
  \ Restaura la posición guardada del cursor en la ventana de salida.
  output-xy 2@ at-xy  ;
: at_first_output
  \ Sitúa el cursor en la posición en que se ha de imprimir la primera frase
  \ (en la parte inferior de la ventana de salida).
  0 last_row 3 - at-xy
  \ 0 dup at-xy
  \ 0 9 at-xy  \ prueba!!!
  ;
: init_output_cursor
  output_window
  at_first_output save_output_cursor
  no_window
  ;
: at_input
  \ Sitúa el cursor en la zona de entrada (la última línea).
  0 last_row at-xy  ;

\ }}} ##########################################################
\ Impresión de textos {{{

\ Indentación de la primera línea de cada párrafo (en caracteres):
2 value /indentation 
\ ¿Indentar también la línea superior de la pantalla?:
variable indent_first_line_too?

: not_first_line?  ( -- ff )  row 0>  ;
: indentation?  ( -- ff )
  \ ¿Indentar la línea actual?
  not_first_line? indent_first_line_too? @ or  ;
: (indent)
  \ Indenta.
  /indentation print_indentation
  ;
: indent
  \ Indenta si es necesario.
  indentation? if  (indent)  then
  ;
: cr+
  print_cr indent 
  ;
: paragraph  ( a u -- )
  \ Imprime un texto justificado como inicio de un párrafo.
  \ a u = Texto
  cr+ print
  ;

\ }}} ##########################################################
\ Pausas {{{

: time?  ( d -- ff )  utime d<  ;
: microseconds  ( u -- )
  \ Espera un número de microsegundos o hasta que se pulse una tecla.
  s>d utime d+
  begin  2dup time? key? 0= or  until
  begin  2dup time? key? or  until
  2drop  ;
: miliseconds  ( u -- )  1000 * microseconds  ;
: seconds  ( u -- )  1000000 * microseconds  ;

\ }}} ##########################################################
\ Título {{{

: title_row  ( u -- u )
  \ Hace un salto de línea y sitúa el cursor en una columna.
  \ u = Columna
  dup cr at-x  ;
: .title
  \ Imprime el título en la posición actual del cursor.
  column
  ."   _" title_row
  ."  /_)    _)_ _ ( _  o  _   _   _   _ o  _" title_row
  ." / / (_( (_ (_) ) ) ( )_) ) ) (_) (  ( (" title_row
  ."                     (            _)   _)" title_row
  drop  ;

40 constant title_width 
4 constant title_height

: margin  ( u1 u2 -- u3 )
  \ Devuelve el margen que hay que dejar para centrar algo en pantalla.
  \ u1 = Medida grande, la de la pantalla (ancho o alto)
  \ u2 = Medida pequeña, la del texto a imprimir (ancho o alto)
  - 2 /  ;
: .centered_title
  \ Imprime el título en el centro de la pantalla.
  cols title_width margin
  rows title_height margin
  at-xy .title  ;
  
\ }}} ##########################################################
\ Datos {{{

variable #sentences  \ Número de frases (en Gforth se inicializa a cero)
defer 'sentences  \ Tabla de las direcciones de las frases

: >sentence>  ( u1 -- u2 )
  \ Convierte el número ordinal de una frase
  \ en su número de elemento en la tabla.
  #sentences @ swap -  ;
: 'sentence  ( u -- a )
  \ Convierte el número ordinal de una frase en su dirección.
  >sentence> cells 'sentences + @  ;
: sentence$  ( u -- a1 u1 )
  \ Devuelve una frase a partir de su número ordinal.
  'sentence count  ;
: .sentence  ( u -- )
  \ Imprime una frase.
  \ u = Número ordinal de la frase 
  output_window
  restore_output_cursor
  sentence$ paragraph
  save_output_cursor
  no_window
  ;

: hs,  ( a u -- a1 )
  \ Compila una cadena en el diccionario y devuelve su dirección.
  here rot rot s,  ;
: sentence:  ( a u "name" -- a1 )
  \ Compila una frase, devuelve su dirección
  \ y crea una constante que devolverá su número ordinal.
  hs,  #sentences 1 over +!  @ constant  ;

\ Crear las frases, definidas en un fichero independiente:
include autohipnosis_sentences.fs

\ En este punto, las direcciones de todas las frases
\ están en la pila y la variable 'sentences#' contiene
\ el número de frases que han sido creadas.
( a1 ... an )

: sentences,  ( a1 ... an -- )
  \ Compila las direcciones de las frases.
  #sentences @ 0  do  ,  loop  ;

\ Tabla para las direcciones de las frases
create ('sentences)  ' ('sentences) is 'sentences
sentences,  \ Rellenar la tabla compilando en el diccionario su contenido

: associated?  ( u a | a u -- ff )
  \ ¿Está una palabra del juego asociada a una frase?
  \ u = Identificador de la frase
  \ a = Dirección de la zona de datos de la palabra
  + c@ 0<>  ;
: associate  ( u a -- )
  \ Asocia una frase a una palabra del juego.
  \ u = Identificador de la frase
  \ a = Dirección de la zona de datos de la palabra
  + true swap c!  ;

: execute_nt  ( i*x nt -- j*x )
  \ Ejecuta el comportamiento en modo de interpretación
  \ de una palabra cuyo nt se proporciona.
  name>int execute  ;
: execute_latest  ( i*x -- j*x )
  \ Ejecuta el comportamiento en modo de interpretación
  \ de la última palabra creada.
  latest execute_nt  ;

: update_term  ( u nt -- )
  \ Asocia una palabra del juego a una frase.
  \ u = Identificador de la frase
  \ nt = Identificador de nombre de la palabra
  \ cr ." update_term ... " show \ depuración!!!
  execute_nt  ( u a )  associate 
  \ cr ." salida de update_term ... " show \ depuración!!!
  ;
\ Alias necesario porque el vocabulario 'forth' no estará
\ visible durante la creación de los términos:
' variable alias term
: create_term_header  ( a u -- )
  \ Crea la cabecera de una palabra del juego.
  \ a u = Nombre de la palabra
  \ cr ." create_term_header ... " show \ depuración!!!
  \ Sistema antiguo:
  \ name-too-short? header, reveal dovar: cfa, 
  \ 2012-06-17 Sistema nuevo:
  s" term" 2swap s& evaluate
  \ cr ." salida de create_term_header ... " show \ depuración!!!
  ;
: create_term_array
  \ Crea la matriz de datos de una palabra del juego.
  \ Cada palabra del juego tiene una matriz para marcar
  \ las frases con las que está asociada.
  \ Para simplificar el código, se usa una matriz de octetos
  \ en lugar de una matriz de bitios: tantos octetos
  \ como frases hayan sido definidas.
  here  #sentences @ dup allot align  erase  ;
: (create_term)  ( a u -- )
  \ Crea una palabra asociada a una frase,
  \ que funciona como una variable
  \ pero que tiene una zona de datos de tantos octetos
  \ como frases hayan sido definidas.
  \ a u = Nombre de la palabra
  \ cr ." (create_term) entrada ( a u )" show \ depuración!!!
  create_term_header create_term_array
  \ cr ." salida de (create_term) ( ) " show \ depuración!!!
  ;
: init_term  ( u -- )
  \ Inicializa la última palabra del juego creada,
  \ asociándola a una frase.
  \ u = Identificador de la frase
  \ cr ." init_term entrada ( u ) " show \ depuración!!!
  execute_latest associate
  \ cr ." salida de init_term ( ) " show \ depuración!!!
  ;
: create_term  ( u a1 u1 -- )
  \ Crea e inicializa una palabra asociada a una frase.
  \ u = Identificador de la frase
  \ a1 u1 = Nombre de la palabra
  \ cr ." create_term entrada ( u a1 u1 ) " show \ depuración!!!
  (create_term) init_term
  \ cr ." create_term salida ( ) " show \ depuración!!!
  ;
: another_term  ( u a1 u1 -- )
  \ Crea o actualiza una palabra asociada a una frase.
  \ u = Identificador de la frase
  \ a1 u1 = Nombre de la palabra
  \ cr ." another_term entrada ( u a1 u1 ) " show \ depuración!!!
  2dup
  \ cr ." another_term before find-name " show \ depuración!!!
  find-name
  \ cr ." another_term after find-name " show \ depuración!!!
  ?dup 
  \ cr ." another_term tras ?dup " show \ depuración!!!
  if  nip nip update_term  else  create_term  then
  \ cr ." another_term final ( ) " show \ depuración!!!
  ;
: parse_term  ( -- a u )
  \ Devuelve la siguiente palabra asociada a una frase.
  begin   parse-name dup 0=
  while   2drop refill 0= abort" Error en el código fuente: falta un '}terms'" 
  repeat  ;
: another_term?  ( -- a u f )
  \ ¿Hay una nueva palabra en la lista?
  \ Toma la siguiente palabra en el flujo de entrada
  \ y comprueba si es el final de la lista de palabras asociadas a una frase.
  \ a u = Palabra encontrada
  \ f = ¿No es el final de la lista?
  parse_term 
  \ 2dup cr ." ************************** " type  \ depuración!!!
  2dup s" }terms" compare
  \ cr ." ( -- a u f )" \ !!!
  ;
: terms{  ( u "name#0" ... "name#n" "}terms" -- )
  \ Crea o actualiza palabras asociadas a una frase.
  \ u = Identificador de frase
  \ cr ." ############################# terms{" show \ depuración!!!
  only game_vocabulary also player_vocabulary definitions
  assert( depth 1 = )
  begin   
    \ cr ." terms{ after begin ... " show \ depuración!!!
    dup another_term? ( u u a1 u1 f )
    \ cr ." terms{ before while ... " show \ depuración!!!
    assert( depth 5 = )
  while   another_term
    assert( depth 1 = )
    \ cr ." terms{ before repeat... " show \ depuración!!!
  repeat  
  assert( depth 4 = )
  \ cr ." antes de 2drop 2drop ... " show \ depuración!!!
  2drop 2drop
  restore_vocabularies  ;

\ Crear los términos, definidos en un fichero independiente:
include autohipnosis_terms.fs

\ Crear los comandos especiales para controlar el juego

also player_vocabulary definitions
: #fin ( -- )
  \ Pendiente!!!
  ;
restore_vocabularies

\ }}} ##########################################################
\ Intérprete de comandos {{{

variable sentence#  \ Frase en curso
variable valid  \ Contador de términos acertados en cada comando

: valid++  (  a -- )
  \ Incrementa la cuenta de aciertos, si procede.
  \ a = Dirección de la zona de datos de un término asociado a una frase
  sentence# @ associated? abs valid +!  ;
: execute_term  ( nt -- )
  \ Executa un término asociado a una frase.
  \ nt = Identificador de nombre del término
  execute_nt  ( a ) valid++  ;
: (evaluate_command)
  \ Analiza la fuente actual con los vocabularios activos,
  \ ejecutando las palabras reconocidas
  \ como términos asociados a una frase.
  begin   
  \ cr ." (evaluate_command) 1 " show... \ depuración!!!
  parse-name ?dup
  \ cr ." (evaluate_command) 2 " show... \ depuración!!!
  while   
  \ cr ." (evaluate_command) 2a " show... \ depuración!!!
  find-name ?dup
  \ cr ." (evaluate_command) 3 " show... \ depuración!!!
  if  execute_term  then
  repeat  drop  ;
: evaluate_command  ( a u -- )
  \ Analiza una cadena con el vocabulario del jugador.
  only player_vocabulary
  ['] (evaluate_command) execute-parsing
  restore_vocabularies  ;
variable testing
: valid?  ( a u -- ff )
  \ ¿Contiene una cadena algún término asociado a la frase actual?
  valid off  evaluate_command  valid @ 0<> 
  testing @ or  ;
: /command  ( -- u )
  \ Longitud máxima de un comando
  cols  ;
: init_command_line
  \ Limpia la línea de comandos y sitúa el cursor.
  0 last_row at-xy trm+erase-line  ;
create 'command /command chars allot align
: command  ( -- a u )
  \ Acepta un comando del jugador.
  init_command_line
  only player_vocabulary
  'command /command accept  'command swap
  restore_vocabularies
  no_window  ;

\ }}} ##########################################################
\ Final {{{

variable success?  \ ¿Se ha completado con éxito el juego?

: the_happy_end
  \ Pendiente!!!
  success? on  ;

\ }}} ##########################################################
\ Ayuda {{{

: curiosities
  \ Pendiente!!!
  s" Curiosidades..." paragraph  ;

: game$  ( -- a u )
  s{ s" juego!!!" s" programa" }s  ;
: the_game$  ( -- a u )
  s" el" game$ s&  ;
: except$  ( -- a u )
  s{ s" excepto" s" salvo" }s  ;
: way$  ( -- a u )
  s{ s" manera" s" forma" }s  ;
: leave$  ( -- a u )
  s{ s" abandonar" s" detener" s" dejar" s" interrumpir" }s  ;
: left$  ( -- a u )
  s{ s" abandonado" s" detenido" s" dejado" s" interrumpido" }s  ;
: pressing$  ( -- a u )
  s{ s" pulsando" s" mediante" }s  ;
: they_make$  ( -- a u )
  s{ s" forman" s" componen" }s  ;
: instructions_0
  \ Instrucciones sobre el objeto del juego.
  s" El" s{ s" programa" s" juego" }s&
  s{ s" mostrará" s" imprimirá" }s&
  s" un texto" s& s" en la pantalla" s?&
  s" y" s&
  s{ s" a continuación" s" después" s" seguidamente" }s?&
  s{ s" esperará" s" se quedará esperando" }s&
  s" una respuesta."  s&
  s{
  s" El" s{ s" juego" s" objetivo" }s& s" consiste en" s&
  s" Lo que" s{ s" has de" s" tienes que" s" hay que" s" debes" }s& s" hacer es" s&
  s" El jugador" s{ s" debe" s" tiene que" }s&
  s{ s" Tienes que" s" Debes" s" Has de" s" Hay que" }s
  s" Tu" s{ s" objetivo" s" misión" }s& s{ s" es" s" será" s" consiste en" }s&
  }s&
  s{ s" responder a" s" escribir una respuesta para" }s&
  s" cada texto" s&
  s{ s" con" s" usando" s" empleando" s" utilizando" s" incluyendo" }s&
  s{ s" al menos" s" por lo menos" }s?&
  s" un sustantivo relacionado con" s&
  s{ s" el mismo" s" él" }s& comma+
  s" pero que no sea" s&
  s{ s" familia de" s" de la misma familia que" }s&
  s" alguna de las palabras" s&
  s{ 
  s" que lo" they_make$ s& 
  s" que" they_make$ s& s" el texto" s&
  s" que" they_make$ s& s" dicho texto" s&
  s{ s" del" s" de dicho" }s s" texto" s&
  }s& period+
  s" El proceso" s&
  s{ s" se repetirá" s" continuará" s" no acabará" s" no terminará" }s&
  s" hasta que todos los textos hayan sido mostrados y respondidos." s&
  paragraph  ;
: instructions_1
  \ Instrucciones sobre el abandono del juego.
  s{
  s" No es posible" leave$ s& the_game$ s& comma+ except$ s& pressing$ s&
  s" El" game$ s& s" no puede ser" s& left$ s& comma+ except$ s& pressing$ s&
  s" La única" way$ s& s" de" s& leave$ s& the_game$ s& s" es pulsar" s&
  }s
  s{ s" la combinación de teclas" s" el atajo de teclado" s" las teclas" }s&
  s" «Ctrl»+«C»," s&
  s{ s" lo que" s" lo cual" }s& s" te" s&
  s{ s" devolverá" s" hará regresar" }s&
  s{ s" a la línea de comandos" s" al intérprete" }s&
  s" de Forth." s&
  paragraph  ;
: instructions_2
  \ Instrucciones sobre el arranque del juego.
  s" Tanto para empezar a jugar ahora como para hacerlo tras haber"
  left$ s& the_game$ s&
  s" puedes" s& s{ s" usar" s" probar" }s&
  s" cualquier palabra que" s&
  s{ s" se te ocurra" s" que te parezca" }s& comma+ s" hasta" s&
  s{ s" encontrar" s" dar con" s" acertar con" }s&
  s{ s" alguna" s" una" }s& s" que" s&
  s{ s" surta efecto" s" funcione" s" sirva" }s& period+
  paragraph    ;
: instructions  ( -- false )
  \ Inacabado!!!
  page s" Instrucciones de Autohipnosis" paragraph
  instructions_0
  instructions_1
  instructions_2  false  ;

\ }}} ##########################################################
\ Menú {{{

: .menu
  \ Imprime el «menú».
  cr s" ¿Qué quieres hacer?" paragraph  ;
: (evaluate_option)
  \ Analiza la fuente actual con los vocabularios activos,
  \ ejecutando las palabras reconocidas.
  begin  parse-name ?dup
  while  find-name ?dup   if  execute_nt  then
  repeat  drop  ;
: evaluate_option  ( a u -- )
  \ Analiza una cadena con el vocabulario del menú. 
  only menu_vocabulary
  ['] (evaluate_option) execute-parsing
  restore_vocabularies  ;
variable finished
: finish
  finished on  ;
: menu  ( -- ff )
  \ Muestra el menú, espera y obedece una opción.
  \ ff = ¿Salir del programa?
  finished off
  .menu command evaluate_option finished @  ;

\ }}} ##########################################################
\ Inicialización {{{

: init/once
  \ Inicialización necesaria antes de la primera partida.
  \ Pendiente!!!
  page  ;
: init/game
  \ Inicialización necesaria antes de cada partida.
  page .centered_title 10 seconds
  init_output_cursor  page  ;

\ }}} ##########################################################
\ Juego {{{

: ask  ( u -- )
  \ Pide al jugador un comando, hasta que recibe uno válido.
  \ u = Número ordinal de la frase actual.
  sentence# !
  begin  command  valid?  until  ;
: step  ( u -- )
  \ Un paso del juego.
  \ u = Número ordinal de la frase actual.
  dup .sentence ask  ;
: game
  \ Bucle de cada partida.
  #sentences @ dup 1
  do  i step  loop
  .sentence  \ Frase final
  the_happy_end  ;
: play
  \ Jugar una partida.
  init/game game   ;

\ }}} ##########################################################
\ Comandos del menú {{{

\ El menú tiene solo tres comandos, cada uno con muchos sinónimos.

also menu_vocabulary definitions

\ Comando «instrucciones»:
' instructions alias ayuda
' instructions alias espera
' instructions alias esperad
' instructions alias esperar
' instructions alias espero
' instructions alias ex
' instructions alias examina
' instructions alias examinad
' instructions alias examinar
' instructions alias examino
' instructions alias examínate
' instructions alias examínome
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
' instructions alias mírome
' instructions alias pista
' instructions alias pistas
' instructions alias registra
' instructions alias registrad
' instructions alias registrar
' instructions alias registro
' instructions alias x
\ Comando «jugar»:
' play alias arranca
' play alias arrancad 
' play alias arrancar 
' play alias arranco
' play alias comenzad
' play alias comenzar
' play alias comienza
' play alias comienzo
' play alias ejecuta
' play alias ejecutad
' play alias ejecutar
' play alias ejecuto
' play alias empezad
' play alias empezar
' play alias empieza
' play alias empiezo
' play alias inicia
' play alias iniciad 
' play alias iniciar 
' play alias inicio
' play alias juego
' play alias jugad
' play alias jugar
' play alias partida
' play alias probad
' play alias probar
' play alias pruebo
\ Comando «fin»:
' finish alias acaba
' finish alias acabad
' finish alias acabar
' finish alias acabo
' finish alias acabose
' finish alias acabó
' finish alias adiós
' finish alias apaga
' finish alias apagad
' finish alias apagar
' finish alias apago
' finish alias apágate
' finish alias cerrad
' finish alias cerrar
' finish alias cierra
' finish alias cierre
' finish alias cierro
' finish alias ciérrate
' finish alias concluid
' finish alias concluir
' finish alias conclusión
' finish alias concluye
' finish alias concluyo
' finish alias desconecta
' finish alias desconectad
' finish alias desconectar
' finish alias desconecto
' finish alias desconexión
' finish alias desconéctate
' finish alias fin
' finish alias final
' finish alias finaliza
' finish alias finalización
' finish alias finalizad
' finish alias finalizar
' finish alias finalizo
' finish alias sal
' finish alias salgo
' finish alias salid
' finish alias salida
' finish alias salir
' finish alias termina
' finish alias terminación
' finish alias terminad
' finish alias terminar
' finish alias termino
' finish alias término

restore_vocabularies

\ }}} ##########################################################
\ Principal {{{

: main
  \ Bucle principal del juego.
  init/once  begin  menu  until  ;

' main alias autohipnosis
' main alias arranca
' main alias arrancad
' main alias arrancar 
' main alias arranco
' main alias comenzad
' main alias comenzar
' main alias comienza
' main alias comienzo
' main alias ejecuta
' main alias ejecutad
' main alias ejecutar
' main alias ejecuto
' main alias ejecútate
' main alias empezad
' main alias empezar
' main alias empieza
' main alias empiezo
' main alias inicia
' main alias iniciad
' main alias iniciar 
' main alias inicio
' main alias iníciate
' main alias juega
' main alias juego
' main alias jugad
' main alias jugar
' main alias partida
' main alias probad
' main alias probar
' main alias prueba
' main alias pruebo
' main alias adelante
' main alias ya
' main alias vamos
' main alias venga

\ autohipnosis

\ }}} ##########################################################
\ Depuración {{{

: .sentences  ( a -- )
  \ Imprime todas las frases con las que está asociado un término.
  \ a = Dirección del término
  \ Ejemplos de uso:
  \   confort .sentences
  \   cambio .sentences
  cr
  #sentences @ 0 ?do
    dup i + c@ if  i sentence$ type cr  then
  loop  drop  ;

\ }}} ##########################################################
\ Notas {{{

false  [if]

Ideas:

Modo de juego por puntos, siempre avanzando, un punto por
acierto.

Pendiente!!!:

Hacer variables los textos de las instrucciones y del presto.

Pedir confirmación de salida, pulsando la barra espaciadora.

[then]

\ }}} ##########################################################
