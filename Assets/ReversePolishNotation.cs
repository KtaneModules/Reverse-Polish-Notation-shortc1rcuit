/*MESSAGE TO ANY FUTURE CODERS:
 PLEASE COMMENT YOUR WORK
 I can't stress how important this is especially with bomb types such as boss modules.
 If you don't it makes it realy hard for somone like me to find out how a module is working so I can learn how to make my own.
 Please comment your work.
 Short_c1rcuit*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using UnityEngine;
using KModkit;

public class ReversePolishNotation : MonoBehaviour
{

	//Gets audio clips and info about the bomb.
	public KMAudio audio;
	public KMBombInfo bomb;

	//Sets up the text meshes so they can be changed later
	public TextMesh problem;
	public TextMesh answer;

	//Defines the keys on the keypad
	public KMSelectable[] keypad;

	//Colours for the stage counter. 0 = white, 1 = green
	public Material[] Stagecolours;

	//Object that show the stage number
	public Renderer[] stages;

	string[][] usedChars =
	{
		new string[]{},
		new string[]{},
		new string[]{}
	};

	//Solution to the equation
	double solution;

	//To stop multiple occurences of a decimal point in an input
	bool decpoint = false;

	//The current stage you're on (0-indexed)
	int stage;

	//Begining time on the bomb
	int startingTime;

	//Logging
	static int moduleIdCounter = 1;
	int moduleId;
	private bool moduleSolved;

	//Twitch help message
#pragma warning disable 414
	private readonly string TwitchHelpMessage = @"Input your answer with “!{0} <input>”. Clear it with “!{0} clr”. Submit it with “!{0} submit”.";
#pragma warning restore 414

	public IEnumerator ProcessTwitchCommand(string command)
	{
		//Sets all text to lower case and removes any white space at the end
		command = command.ToLowerInvariant().Trim();

		//Submits the input
		if (command == "submit")
		{
			yield return null;
			keypad[11].OnInteract();
		}
		//Clears the input
		else if (command == "clr")
		{
			yield return null;
			keypad[12].OnInteract();
		}
		//Checks to make sure that it is a valid number
		else if (Regex.IsMatch(command, @"^\d+(?:\.\d+)?$"))
		{
			foreach (char character in command)
			{
				//If it is a digit
				if (Regex.IsMatch(character.ToString(), @"\d"))
				{
					yield return null;
					keypad[int.Parse(character.ToString())].OnInteract();
				}
				//If it is a point
				else
				{
					yield return null;
					keypad[10].OnInteract();
				}
			}
		}
		//If it's an invalid command
		else
		{
			yield return "sendtochaterror The command you inputted is incorrect.";
		}
	}

	private IEnumerator TwitchHandleForcedSolve()
    {	
		for (int st = stage; st < 3; st++) // For each stage, starting at the current stage...
        {
            int currentIndex = 0;
			// Declare "currentIndex" as our current position in the input.
			// (0 entered digits? currentIndex = 0. 3 entered chracters? currentIndex = 3.)
			// The purpose of "currentIndex" is to try to press the least amount of keys as possible.
			// For example, if we have "3.6" as our input, and the answer is "3.667", all we should do is press 6, 7, and submit.

			// For each character in our current input...
			for (int inputCheck = 0; inputCheck < answer.text.Length; inputCheck++)
            {
				// If our current input is longer than the solution, or,
				// If the "inputCheck" position of our input is not equal to the "inputCheck" position of the solution...
				if (answer.text.Length > solution.ToString().Length || answer.text[inputCheck] != solution.ToString()[inputCheck])
				{
					// Reset, and set currentIndex back to 0. (As we need to enter the input from the start.)
					keypad[12].OnInteract();
					currentIndex = 0;
					yield return new WaitForSeconds(0.2f);
					// Stop iterating through the check if our input is incorrect.
					goto doInput;
				}
				// Otherwise, add 1 to currentIndex.
				currentIndex++;
            }

			doInput:
			// For each character in the solution, starting at the position of our current input...
			for (int input = currentIndex; input < solution.ToString().Length; input++)
            {
				// Press the buttons that result in our solution.
                keypad["0123456789.".IndexOf(solution.ToString()[input])].OnInteract();
				yield return new WaitForSeconds(0.1f);
            }
			// Press the submit button.
			keypad[11].OnInteract();

			// If the module isn't solved, wait. (This avoids an extra 0.2 second wait after the module is solved.)
			if (!moduleSolved)
				yield return new WaitForSeconds(0.2f);
        }
    }

	void Awake()
	{
		problem.text = "";
		answer.text = "";

		//More logging stuff
		moduleId = moduleIdCounter++;

		//Takes the keys and gives them their methods
		foreach (KMSelectable key in keypad)
		{
			KMSelectable pressedKey = key;
			key.OnInteract += delegate () { KeyPress(pressedKey); return false; };
		}
		GetComponent<KMBombModule>().OnActivate += OnActivate;
	}

	void OnActivate()
	{
		startingTime = (int)bomb.GetTime() / 60;
		StartCoroutine(UpdateStages());
	}

	IEnumerator UpdateStages()
	{
		//For loop repeats 3 times for 3 stages
		for (int i = 0; i < 3; i++)
		{
			//Sets the material for the stage counters
			if (i != 0)
			{
				stages[i - 1].material = Stagecolours[1];
			}

			stages[i].material = Stagecolours[0];

			//Stage 1 has 5 characters, then 7, then 9
			solution = SolveStage(GenerateStage((2 * i) + 5));

			//Waits for the stage to update
			while (stage == i)
			{
				yield return null;
			}
		}

		//Sets the material for the stage counters
		stages[2].material = Stagecolours[1];

		//Solves the module
		problem.text = "NICE";
		answer.text = "ONE";

		moduleSolved = true;
		GetComponent<KMBombModule>().HandlePass();
		audio.PlaySoundAtTransform("ding_ding", transform);
		Debug.LogFormat("[Reverse Polish Notation #{0}] Module solved.", moduleId);
	}

	string[] GenerateStage(int size)
	{
		//maxJumps is the max number of moves forward.
		int maxJumps = 2;
		int position = 1;

		//The RPN equation to be solved
		string[] equation = new string[size];
		equation[0] = GenerateRandomNumber();
		equation[1] = GenerateRandomNumber();

		//Pass 1: Goes through the code and places in all of the numbers/letters
		for (int i = 0; i < Math.Ceiling(size / 2m) - 2; i++)
		{
			int steps = UnityEngine.Random.Range(0, maxJumps);
			maxJumps += 1 - steps;
			position += steps + 1;
			equation[position] = GenerateRandomNumber();
		}

		//Pass 2: Adds in operations
		for (int i = 0; i < size; i++)
		{
			if (equation[i] == null)
			{
				equation[i] = GenerateRandomOperation();
			}
		}

		problem.text = string.Join(" ", equation);
		Debug.LogFormat("[Reverse Polish Notation #{0}] The equation is {1}", moduleId, string.Join(" ", equation));

		usedChars[stage] = Array.FindAll(equation, x => Regex.IsMatch(x, @"\w"));

		return equation;
	}

	//Returns either a number from 0-9 or a letter from A-G
	string GenerateRandomNumber()
	{
		if (UnityEngine.Random.Range(0, 2) == 0)
		{
			return UnityEngine.Random.Range(0, 10).ToString();
		}
		else
		{
			return ((char)UnityEngine.Random.Range(65, 72)).ToString();
		}
	}

	//Generates either +, -,* or /
	string GenerateRandomOperation()
	{
		string operation;

		//As , and . are mixed in with the other operations so this loop filters it out
		do
		{
			operation = ((char)UnityEngine.Random.Range(42, 48)).ToString();
		} while (operation == "," || operation == ".");

		return operation;
	}

	double SolveStage(string[] equation)
	{
		List<double> stack = new List<double>();
		for (int i = 0; i < equation.Length; i++)
		{
			//If it's a digit
			if (Regex.IsMatch(equation[i], @"\d"))
			{
				//Adds it to the stack
				stack.Add(double.Parse(equation[i]));
				Debug.LogFormat("[Reverse Polish Notation #{0}] {1} is added to the stack", moduleId, equation[i]);
			}
			else if (Regex.IsMatch(equation[i], @"[A-G]"))
			{
				//Works out what the letter means and then adds it to the stack
				stack.Add(DecipherLetter(equation[i], equation));
				//After the calculation if any number becomes 0 add 1 to it
				if (stack[stack.Count - 1] == 0)
				{
					stack[stack.Count - 1] = 1;
				}
				Debug.LogFormat("[Reverse Polish Notation #{0}] {1} returns {2}", moduleId, equation[i], stack[stack.Count - 1]);
			}
			else
			{
				//Takes the top two items off the stack, does the operation on them and then adds the result to the top of the stack
				if (equation[i] == "+")
				{
					Debug.LogFormat("[Reverse Polish Notation #{0}] {1} plus {2} is {3}", moduleId, stack[stack.Count - 2], stack[stack.Count - 1], stack[stack.Count - 2] + stack[stack.Count - 1]);
					stack[stack.Count - 2] = stack[stack.Count - 2] + stack[stack.Count - 1];
					stack.RemoveAt(stack.Count - 1);
				}
				else if (equation[i] == "-")
				{
					Debug.LogFormat("[Reverse Polish Notation #{0}] {1} minus {2} is {3}", moduleId, stack[stack.Count - 2], stack[stack.Count - 1], stack[stack.Count - 2] - stack[stack.Count - 1]);
					stack[stack.Count - 2] = stack[stack.Count - 2] - stack[stack.Count - 1];
					stack.RemoveAt(stack.Count - 1);
				}
				else if (equation[i] == "*")
				{
					Debug.LogFormat("[Reverse Polish Notation #{0}] {1} multiplied by {2} is {3}", moduleId, stack[stack.Count - 2], stack[stack.Count - 1], stack[stack.Count - 2] * stack[stack.Count - 1]);
					stack[stack.Count - 2] = stack[stack.Count - 2] * stack[stack.Count - 1];
					stack.RemoveAt(stack.Count - 1);
				}
				else if (equation[i] == "/")
				{
					if (stack[stack.Count - 1] == 0)
					{
						Debug.LogFormat("[Reverse Polish Notation #{0}] {1} divided by {2} is {3}", moduleId, stack[stack.Count - 2], stack[stack.Count - 1], "0");
						stack[stack.Count - 2] = 0;
						stack.RemoveAt(stack.Count - 1);
					}
					else
					{
						Debug.LogFormat("[Reverse Polish Notation #{0}] {1} divided by {2} is {3}", moduleId, stack[stack.Count - 2], stack[stack.Count - 1], stack[stack.Count - 2] / stack[stack.Count - 1]);
						stack[stack.Count - 2] = stack[stack.Count - 2] / stack[stack.Count - 1];
						stack.RemoveAt(stack.Count - 1);
					}
				}
			}
		}
		Debug.LogFormat("[Reverse Polish Notation #{0}] The solution is {1}", moduleId, (double)Math.Round((decimal)Math.Abs(stack[0]) % 1000000, 3, MidpointRounding.AwayFromZero));
		return (double)Math.Round((decimal)Math.Abs(stack[0]) % 1000000, 3, MidpointRounding.AwayFromZero);
	}

	double DecipherLetter(string letter, string[] equation)
	{
		//See the manual for reference of what each case is doing (default is G)
		switch (letter)
		{
			case "A":
				return bomb.GetSolvableModuleNames().Count * bomb.GetBatteryCount();
			case "B":
				return Math.Pow(bomb.GetPortCount() - bomb.GetIndicators().Count(), 2);
			case "C":
				if (bomb.GetSerialNumberNumbers().Last() == 0)
				{
					return 0;
				}
				else
				{
					return Math.Floor((double)(bomb.GetBatteryHolderCount() + 2) / bomb.GetSerialNumberNumbers().Last());
				}
			case "D":
				double total = 0;
				foreach (string item in equation)
				{
					if (Regex.IsMatch(item, @"\d"))
					{
						total += double.Parse(item);
					}
				}
				total += 10;
				return total;
			case "E":
				int result = bomb.GetPortPlates().Count(x => x.Contains("Parallel")) + bomb.GetPortPlates().Count(x => x.Contains("Serial"));
				if (stage % 2 == 0)
				{
					return result * double.Parse(bomb.GetSerialNumber()[2].ToString());
				}
				else
				{
					return result + double.Parse(bomb.GetSerialNumber()[2].ToString());
				}
			case "F":
				double n = (bomb.GetPortPlateCount() + bomb.GetPortPlates().Count(x => x.Contains("PS2")) + bomb.GetPortPlates().Count(x => x.Contains("RJ45"))) % 9;
				return n * (n + 1) / 2;
			default:
				return LunarAddition(bomb.GetSolvableModuleNames().Count(), startingTime);
		}
	}

	int LunarAddition(int num1, int num2)
	{
		//Finds out the longer length of the two numbers
		int length = (int)Math.Max(Math.Log10(num1), Math.Log10(num2)) + 1;
		int total = 0;

		//Finds out the larger number of the two
		for (int i = 0; i < length; i++)
		{
			total += (int)(Math.Floor(Math.Max(num1 / Math.Pow(10, i) % 10, (num2 / Math.Pow(10, i)) % 10)) * Math.Pow(10, i));
		}

		return total;
	}

	void KeyPress(KMSelectable key)
	{
		if (moduleSolved) return;

		//Makes the bomb move when you press it
		key.AddInteractionPunch();

		//Makes a sound when you press the button.
		audio.PlaySoundAtTransform("keyStroke", transform);

		//As the value of each number on the keypad is equivalent to their position in the array, I can get the button's position and use that to work out it's value.
		int number = Array.IndexOf(keypad, key);

		//If CLR is pressed
		if (number == 12)
		{
			//Clear the screen
			answer.text = "";
			decpoint = false;
		}
		//If OK is pressed
		else if (number == 11)
		{
			//If they answered correctly
			if (answer.text == solution.ToString())
			{
				audio.PlaySoundAtTransform("ding", transform);
				Debug.LogFormat("[Reverse Polish Notation #{0}] You submitted {1}, when the answer was {2}. Correct.", moduleId, answer.text, solution.ToString());
				answer.text = "";
				decpoint = false;
				//Increases the stage by one, which then causes the subrouting to generate a new stage
				stage += 1;
			}
			//If they answered incorrectly
			else
			{
				//Gives a strike
				GetComponent<KMBombModule>().HandleStrike();
				audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.Strike, transform);
				Debug.LogFormat("[Reverse Polish Notation #{0}] You submitted {1}, when the answer was {2}. Incorrect.", moduleId, answer.text, solution.ToString());
				answer.text = "";
				decpoint = false;
			}
		}
		//If . was pressed
		else if (number == 10)
		{
			//This if statement stops anyone putting 2 . in their answer
			if (!decpoint)
			{
				answer.text += ".";
				decpoint = true;
			}
		}
		else
		{
			//Upper limit for the text size to stop text going into other modules
			//(Looking at you Xatra)
			if (answer.text.Length < 10)
			{
				//Adds the number to the end of the sting
				answer.text += number.ToString();
			}
		}
	}
}