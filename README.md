## MindustryFunctionDraftTranslator
This translator just finishes `.minfndft` files to `.min` files, which both is just txt UTF-8 files
generally mindastry instructions file contains.

The `.min` file is ready to be copied into processor (you need to open it by usually text redactor) set of instructions.

The `.minfndft` is my own notation for mindastry function draft instructions (which is still text file).

#### .minfndft notation
Generally `.min` file.
First of all, it contains instruction and draft-things separator which is current single `#` char.
Please do not place it on non-instruction line.

Secondly, there is 2 notations for label and pointer: just a integer and `->` with integer. Some examples:

`print "All " #6`

`op sub trueWay @counter n #->6`

`op sub falseWay @counter n #->7 #9`

Third, it contains operation instructions with @counter ends with `n`.

###### Using
First of all, you need jump-like instruction with counter, which is, for example:

`op sub trueWay @counter n`

Where `n` is line you want instruction to jump. So you don't want to fill `n` manually. So you need
label and pointer to set the program what to do.

To set label just add separator and number of its label. Please use unique values for that. 

For example:

`print "All "     #6`

If you want to jump on these instruction, you need to place pointer into jump-like command:
`op sub trueWay @counter n #->6`.
Please remember: if labeled instruction is upper, use `sub` parameter in mindastry instruction,
or `add` if lower accordingly. I might add auto-replasement later for that.

There is `sub` so labeled line is upper.

Now run the program and get n automatically.

If your line contains label and pointer, you need to place it in strict sequence:

`op sub falseWay @counter n #->7 #9` - pointer first, label the second.

## Using program
There is two ways to run a program: by itself or from cmd.
#### By itself
Will finish all functions drafts in current program folder. Please remember that program is not only executable file.
#### From cmd
You can run program from cmd by one or two parameters. It might requers administrator rules.
###### One parameter
Will finish all fuctions drafts in current cmd path. So parameter is current MindustryFunctionDraftTranslator.exe location.
###### Two parameters
Will finish single file. So the second param is file path.
