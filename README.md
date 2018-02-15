# PeriodiBOT
Bot multipropósito para wikis compatible con MONO.

Simplemente quise compartir el código de mi BOT para que cualquiera pueda usarlo y aportar a su mejora.
El código tiene mucho por mejorar y no es en lo absoluto eficiente, en su desarrollo he priorizado la simplicidad hasta un cierto nivel.

## Preguntas frecuentes:
- ¿Porque hay partes en inglés y otras en español?

R: Por lo general escribo los programas en inglés, por costumbre ha quedado así, pero los "summary" que describen las funciones están en español.

- ¿Porque en IRC?

R: Gusto personal, así se puede controlar al BOT desde cualquier lugar con conexión a Internet.

- ¡Pero que tontería! ¿Qué es eso de TextInBetween? ¿no podrías simplemente haber usado un Parser para XML o Json?

R: En primera instancia, lo hice así, pero sin importar de qué forma se implemente, no tenían el mismo comportamiento bajo .NET y MONO. Al final decidí ir por lo simple: Las respuestas de Wikipedia en formato JSON siguen un formato regular, aprovecho eso para usar expresiones regulares y extraer los parámetros necesarios. ¡Y quién lo diría! funciona perfectamente bien tanto en MONO como en .NET Bajo Windows o Unix-Linux.

- Hay demasiadas funciones que podrían eliminarse si se usa una expresión lambda.

R: Si bien es cierto, prefiero no hacerlo porque hace que el código sea menos legible. Es más fácil leer Suma(1,2) que Function(x,y)(ETC...).

- ¿Porque no en X lenguaje de programación?

R: Me manejo más en .NET y excepto por ciertas cosas menores, C# y VB.NET son capaces de lo mismo. Elegí VB porque es más fácil de entender para principiantes en general (He de ahí BASIC: Begginers All purpose Symbolic Instruction Code, o Código de instrucciones simbólico multipropósito enfocado a principiantes). El lenguaje tiene su belleza sintáctica que lo hace perfecto para RAD (Rapid Application Development).

- Esto es una basura, ¡podría hacer algo mucho mejor, y con ponys!

R: Pues eres libre de hacerlo, nadie lo impide. Si quieres aportar a mejorar el código, pues ya está abierto :).
