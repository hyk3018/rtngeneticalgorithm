using System;
using System.Collections.Generic;
using System.Xml;


namespace RTNGeneticAlgorithm
{
    
    class Program
    {
        static void Main(string[] args)
        {
            Console.ReadLine();
            SentenceGA ga = new SentenceGA("C:\\Users\\Tommy\\Desktop\\CS\\RTNGeneticAlgorithm\\RTNGeneticAlgorithm\\words.xml",1000,6);
            Console.ReadLine();
            Console.ReadLine();
        }
    }

    // Class deals with the list of words that the program can choose from
    class WordList
    {
        public readonly string[,] wordList;
        public readonly int wordCount = 0;

        // Loads words fomr XML file
        public WordList(string wordPath)
        {
            XmlDocument wordXML = new XmlDocument();
            wordXML.Load(wordPath);

            foreach (XmlNode wordType in wordXML.ChildNodes[1])
            {
                foreach (XmlNode word in wordType)
                {
                    wordCount += 1;
                }
            }

            // nx2 array, 1 to store word, other stores word type

            wordList = new string[wordCount, 2];
            int wordCounter = 0;
            foreach (XmlNode wordType in wordXML.ChildNodes[1])
            {
                foreach (XmlNode word in wordType)
                {
                    wordList[wordCounter, 0] = word.InnerText;
                    wordList[wordCounter, 1] = wordType.Name;
                    wordCounter += 1;
                }
            }
        }

        public string GetWordType(string word)
        {
            for (int i=0; i < wordCount; i++)
            {
                if (wordList[i, 0] == word)
                    return wordList[i, 1];
            }
            return "";
        }

    }

    class SentenceGA
    {
        private WordList _wordList;
        private string[,] _sentences;

        private RTN ornateRTN;
        private RTN fancyRTN;
        private Random r = new Random();

        private int _populationSize;
        private int _sentenceLength;

        public SentenceGA(string wordListPath, int populationSize, int sentenceLength)
        {
            _populationSize = populationSize;
            _sentenceLength = sentenceLength;

            _wordList = new WordList(wordListPath);
            ornateRTN = Parse("C:\\Users\\Tommy\\Desktop\\CS\\rtngeneticalgorithm\\"
                            + "rtngeneticalgorithm\\rtn.xml", "ornate_noun");
            fancyRTN = Parse("C:\\Users\\Tommy\\Desktop\\CS\\rtngeneticalgorithm\\"
                            + "rtngeneticalgorithm\\rtn.xml", "fancy_noun");

            // Main GA part
            Generation();
            for (int i = 0; i < 100; i++)
            {
                Evaluation();
                Reproduction();
                PrintSentences();
            }
        }

        // GA Methods
        private void Generation()
        {
            // Randomly generate sentences from this list of words, with no regards to grammar

            _sentences = new string[_populationSize, _sentenceLength+2];

            for (int i = 0; i < _populationSize; i++)
            {
                string[] sentence = new string[_sentenceLength];

                for (int j = 0; j < _sentenceLength; j++)
                {
                    _sentences[i, j] = _wordList.wordList[r.Next(0, _wordList.wordCount),0];
                }
            }
        }

        private void Evaluation(bool elitism=true)
        {
            int cumulativeFrontFitness = 0;
            int cumulativeBackFitness = 0;

            // Get the fitness of each sentence
            for (int sentenceNumber = 0; sentenceNumber < _populationSize; sentenceNumber++)
            {
                string[] currentSentence = new string[_sentenceLength];
                for (int wordNumber = 0; wordNumber < _sentenceLength; wordNumber++)
                    currentSentence[wordNumber] = _sentences[sentenceNumber, wordNumber];

                // Fitness from front
                int frontFitness = Math.Abs(GetFrontFitness(currentSentence, fancyRTN));
                cumulativeFrontFitness += frontFitness;
                _sentences[sentenceNumber, _sentenceLength] = frontFitness.ToString();

                // Fitness from back
                int backFitness = Math.Abs(GetBackFitness(ReverseSentence(currentSentence), fancyRTN));
                cumulativeBackFitness += backFitness;
                _sentences[sentenceNumber,_sentenceLength+1] = backFitness.ToString();
            }

            if (elitism)
            {
                int[] eliteCumulativeFitness = Elitism(cumulativeFrontFitness, cumulativeBackFitness);
                Selection(eliteCumulativeFitness[0], eliteCumulativeFitness[1]);
            }
            else
            {
                Selection(cumulativeFrontFitness, cumulativeBackFitness);
            }

        }

        private int[] Elitism(int cumuFront, int cumuBack)
        {
            // Find the last sentences with highest front and back fitnesses
            int highestFront = 0;
            int highestBack = 0;

            for (int i=0; i < _populationSize; i++)
            {
                if (Int32.Parse(_sentences[i, _sentenceLength]) >= highestFront)
                    highestFront = Int32.Parse(_sentences[i, _sentenceLength]);
                if (Int32.Parse(_sentences[i, _sentenceLength + 1]) >= highestBack)
                    highestBack = Int32.Parse(_sentences[i, _sentenceLength + 1]);
            }

            List<int> elitesFront = new List<int>();
            List<int> elitesBack = new List<int>();

            for (int i=0; i < _populationSize; i++)
            {
                if (Int32.Parse(_sentences[i, _sentenceLength]) == highestFront)
                    elitesFront.Add(i);
                if (Int32.Parse(_sentences[i, _sentenceLength + 1]) == highestBack)
                    elitesBack.Add(i);
            }

            highestFront = elitesFront[r.Next(elitesFront.Count)];
            highestBack = elitesBack[r.Next(elitesBack.Count)];

            // Set cumulative fitnesses to double the current minus the highest
            int[] cumulativeFitnesses = new int[2];
            cumulativeFitnesses[0] = (cumuFront - Int32.Parse(_sentences[highestFront, _sentenceLength])) * 2;
            cumulativeFitnesses[1] = (cumuBack - Int32.Parse(_sentences[highestBack, _sentenceLength+1])) * 2;

            // Set fitness of highest and lowest to be cumulative minus current so it has 0.5 chance of being selected
            _sentences[highestFront, _sentenceLength] = ((cumuFront - Int32.Parse(_sentences[highestFront, _sentenceLength])) * 1).ToString();
            _sentences[highestBack, _sentenceLength+1] = ((cumuBack - Int32.Parse(_sentences[highestBack, _sentenceLength+1])) * 1).ToString();

            return cumulativeFitnesses;
        }

        private void Selection(int cumuFront, int cumuBack)
        {
            string[] fittestFront = new string[_sentenceLength+2];
            string[] fittestBack = new string[_sentenceLength+2];

            // Generate random number, sentence with cumulative frequency range containing this number is the fittest
            int parentCounter = r.Next(cumuFront);
            int currentTotal = 0;
            int firstParentNumber = 0;

            for (int i=0; i < _populationSize; i++)
            {
                currentTotal += Int32.Parse(_sentences[i, _sentenceLength]);
                if (currentTotal >= parentCounter)
                {
                    firstParentNumber = i;
                    Console.WriteLine("Fit front score = {0}", _sentences[i, _sentenceLength]);
                    for (int j=0; j < _sentenceLength+2; j++)
                    {
                        fittestFront[j] = _sentences[i, j];
                    }
                    break;
                }
            }

            // Same as first parent but prevent use of the first as also the second parent
            bool keepGoing = true;
            while (keepGoing)
            {
                keepGoing = false;
                parentCounter = r.Next(cumuBack);
                currentTotal = 0;
                for (int i = 0; i < _populationSize; i++)
                {
                    currentTotal += Int32.Parse(_sentences[i, _sentenceLength + 1]);
                    if (currentTotal >= parentCounter)
                    {
                        if (i == firstParentNumber)
                        {
                            keepGoing = true;
                            break;
                        }
                        Console.WriteLine("Fit back score = {0}", _sentences[i, _sentenceLength + 1]);
                        for (int j = 0; j < _sentenceLength + 2; j++)
                        {
                            fittestBack[j] = _sentences[i, j];
                        }
                        break;
                    }
                }
            }

            _sentences = new string[2, _sentenceLength+2];
            
            for (int i=0; i < _sentenceLength+2; i++)
            {
                _sentences[0, i] = fittestFront[i];
                _sentences[1, i] = fittestBack[i];
            }
        }

        private int GetFrontFitness(string[] sentence, RTN network)
        {
            int fitness = 0;

            int currentWord = 0;
            string currentNode = "begin";

            // While the sentence is deemed fit, keep checking

            while (true)
            {
                // Get the nodes which current node goes to
                List<string> currentTo = network.GetNode(currentNode).GetTo();

                // If reached the end of sentence, check if end of network
                // If so, deem it fit and return positive fitness
                // Otherwise return negative fitness
                if (currentWord == sentence.Length)
                {
                    foreach (string to in currentTo)
                    {
                        if (to == "end")
                        {
                            return fitness;
                        }
                    }

                    return fitness * -1;
                }
                else
                {
                    // Check if the current word is of any significance, such as being an end node, an expand node or a next node
                    // If none, return negative, unfit
                    // Expand nodes will always be checked last - non recursive nodes have priority to prevent unnecessary recursion
                    bool used = false;
                    foreach (string to in currentTo)
                    {
                        if (to == "end")
                        {
                            return fitness;
                        }

                        // For multiple nodes in a network with the same type, will be more useful with more specific and robust networks
                        string generalTo = "";

                        if (to == "verb_1" || to == "verb_2")
                            generalTo = "verb";
                        else
                            generalTo = to;

                        if (_wordList.GetWordType(sentence[currentWord]) == generalTo)
                        {
                            used = true;
                            fitness += 1;
                            currentWord += 1;
                            currentNode = to;
                            break;
                        }
                        else if (network.GetNode(to)._expand)
                        {
                            used = true;

                            // When expanding the node, take sub sentence as input, starting from current word
                            string[] subSentence = new string[sentence.Length - currentWord];

                            for (int i = currentWord; i < sentence.Length; i++)
                            {
                                subSentence[i - currentWord] = sentence[i];
                            }

                            int subFitness;
                            if (to == "ornate_noun")
                                subFitness = GetFrontFitness(subSentence, ornateRTN);
                            else
                                subFitness = GetFrontFitness(subSentence, fancyRTN);
                            fitness += Math.Abs(subFitness);

                            // If the subsentence was unfit, return negative fitness otherwise keep going
                            if (subFitness <= 0)
                            {
                                return fitness * -1;
                            }
                            else
                            {
                                currentWord += subFitness;
                                currentNode = to;
                                break;
                            }
                        }
                        else
                        {
                            used = false;
                        }
                    }
                    if (!used)
                        return fitness * -1;
                }
            }
        }

        private int GetBackFitness(string[] sentence, RTN network)
        {
            int fitness = 0;

            int currentWord = 0;
            string currentNode = "end";

            while (true)
            {
                List<string> currentFrom = network.GetNode(currentNode).GetFrom();
                if (currentWord == sentence.Length)
                {
                    foreach (string from in currentFrom)
                    {
                        if (from == "begin")
                        {
                            return fitness;
                        }
                    }

                    return fitness * -1;
                }
                else
                {
                    bool used = false;
                    foreach (string from in currentFrom)
                    {
                        if (from == "begin")
                        {
                            return fitness;
                        }

                        string generalFrom = "";

                        if (from == "verb_1" || from == "verb_2")
                            generalFrom = "verb";
                        else
                            generalFrom = from;

                        if (_wordList.GetWordType(sentence[currentWord]) == generalFrom)
                        {
                            used = true;
                            fitness += 1;
                            currentWord += 1;
                            currentNode = from;
                            break;
                        }
                        else if (network.GetNode(from)._expand)
                        {
                            used = true;
                            string[] subSentence = new string[sentence.Length - currentWord];

                            for (int i = currentWord; i < sentence.Length; i++)
                            {
                                subSentence[i - currentWord] = sentence[i];
                            }

                            int subFitness;
                            if (from == "ornate_noun" || from == "ornate_noun_2")
                                subFitness = GetBackFitness(subSentence, ornateRTN);
                            else
                                subFitness = GetBackFitness(subSentence, fancyRTN);
                            fitness += Math.Abs(subFitness);

                            if (subFitness <= 0)
                            {
                                return fitness * -1;
                            }
                            else
                            {
                                currentWord += subFitness;
                                currentNode = from;
                                break;
                            }
                        }
                        else
                        {
                            used = false;
                        }
                    }
                    if (!used)
                        return fitness * -1;
                }
            }
        }

        private void Reproduction()
        {
            PrintSentences("2");
            string[,] nextGeneration = new string[_populationSize, _sentenceLength + 2];

            for (int i=0; i < _populationSize; i++)
            {
                int crossoverMechanism = r.Next(675);

                if (crossoverMechanism < 300)
                {
                    // Split mechanism at the front fitness of front fittest or back fitness of back fittest, rest fitter parts of unused parent
                    int splitPosition = r.Next(2);

                    if (splitPosition == 0)
                        splitPosition = Int32.Parse(_sentences[0, _sentenceLength]);
                    else
                        splitPosition = _sentenceLength - Int32.Parse(_sentences[1, _sentenceLength + 1]);

                    for (int j=0; j < _sentenceLength; j++)
                    {
                        if (r.Next(100) < 15)
                            nextGeneration[i, j] = _wordList.wordList[r.Next(_wordList.wordCount), 0];
                        else if (j < splitPosition)
                            nextGeneration[i, j] = _sentences[0, j];
                        else
                            nextGeneration[i, j] = _sentences[1, j];
                    }
                }
                else if (crossoverMechanism < 600)
                {
                    // Split mechanism similar to previous but alternates between 2 parents after split point
                    int splitPosition = r.Next(2);

                    if (splitPosition == 0)
                    {
                        splitPosition = Int32.Parse(_sentences[0, _sentenceLength]);

                        for (int j=0; j < _sentenceLength; j++)
                        {
                            if (r.Next(100) < 15)
                                nextGeneration[i, j] = _wordList.wordList[r.Next(_wordList.wordCount), 0];
                            else if (j < splitPosition)
                                nextGeneration[i, j] = _sentences[0, j];
                            else
                                nextGeneration[i, j] = _sentences[1 - ((j - splitPosition) % 2), j];
                        }
                    }
                    else
                    {
                        splitPosition = _sentenceLength - Int32.Parse(_sentences[1, _sentenceLength + 1]);

                        for (int j=0; j < _sentenceLength; j++)
                        {
                            if (r.Next(100) < 15)
                                nextGeneration[i, j] = _wordList.wordList[r.Next(_wordList.wordCount), 0];
                            else if (j < splitPosition)
                                nextGeneration[i, j] = _sentences[1 - ((splitPosition - j) % 2), j];
                            else
                                nextGeneration[i, j] = _sentences[1, j];
                        }
                    }
                }
                else
                {
                    // Alternate between both parents genes for each word
                    for (int j=0; j < _sentenceLength; j++)
                    {
                        nextGeneration[i, j] = _sentences[r.Next(2), j];
                    }
                }
            }

            _sentences = nextGeneration;
        }

        // Debug methods
        public void PrintSentences(string flag="")
        {
            if (flag == "")
            {
                Console.WriteLine("?");
                for (int i = 0; i < _populationSize; i++)
                {
                    for (int j = 0; j < _sentenceLength + 2; j++)
                    {
                        Console.Write(_sentences[i, j]);
                        Console.Write(" ");
                    }
                    Console.Write("\n");
                }
            }
            else if (flag == "2")
            {
                Console.WriteLine("?");
                for (int i = 0; i < 2; i++)
                {
                    for (int j = 0; j < _sentenceLength; j++)
                    {
                        Console.Write(_sentences[i, j]);
                        Console.Write(" ");
                    }
                    Console.Write("\n");
                }
            }
        }

        private void TestSentence()
        {
            string[] sentence = new string[10];
            sentence[0] = "THE";
            sentence[1] = "STRANGE";
            sentence[2] = "BAGELS";
            sentence[3] = "THAT";
            sentence[4] = "THE";
            sentence[5] = "PURPLE";
            sentence[6] = "COW";
            sentence[7] = "WITHOUT";
            sentence[8] = "HORNS";
            sentence[9] = "GOBBLED";

            foreach (string word in sentence)
            {
                Console.Write(word + " ");
            }
            Console.WriteLine(Math.Abs(GetFrontFitness(sentence, fancyRTN)) + " ");

            string[] reverseSentence = new string[sentence.Length];
            for (int i=0; i < sentence.Length; i++)
            {
                reverseSentence[i] = sentence[(sentence.Length - 1) - i];
            }

            foreach (string word in reverseSentence)
            {
                Console.Write(word + " ");
            }
            Console.WriteLine(Math.Abs(GetBackFitness(reverseSentence, fancyRTN)) + " ");
        }

        // Misc methods
        private RTN Parse(string filePath, string networkName)
        {
            XmlDocument RTNFile = new XmlDocument();
            RTNFile.Load(filePath);

            RTN newNetwork = new RTN(networkName);

            foreach (XmlNode network in RTNFile.ChildNodes[1])
            {
                if (network.Attributes[0].Value == networkName)
                {
                    foreach (XmlNode node in network)
                    {
                        Node newNode;
                        if (node.Attributes[1].Value == "true")
                        {
                            newNode = new Node(node.Attributes[0].Value, true);
                        }
                        else
                        {
                            newNode = new Node(node.Attributes[0].Value, false);
                        }

                        foreach (XmlNode from in node.ChildNodes[0])
                        {
                            newNode.AddFrom(from.InnerText);
                        }

                        foreach (XmlNode to in node.ChildNodes[1])
                        {
                            newNode.AddTo(to.InnerText);
                        }

                        newNetwork.AddNode(newNode);
                    }
                }
            }

            return newNetwork;
        }

        private string[] ReverseSentence(string[] sentence)
        {
            string[] reverseSentence = new string[sentence.Length];
            for (int i = 0; i < sentence.Length; i++)
            {
                reverseSentence[i] = sentence[(sentence.Length - 1) - i];
            }

            return reverseSentence;
        }
    }

    class Node
    {
        public readonly string _name;
        public readonly bool _expand;
        private List<string> _from;
        private List<string> _to;

        public Node(string name,bool expand)
        {
            _name = name;
            _expand = expand;
            _from = new List<string>();
            _to = new List<string>();
        }

        public void AddTo(string name)
        {
            _to.Add(name);
        }

        public void AddFrom(string name)
        {
            _from.Add(name);
        }

        public List<string> GetFrom()
        {
            return _from;
        }

        public List<string> GetTo()
        {
            return _to;
        }
    }

    class RTN
    {
        private Dictionary<string,Node> _network;
        private string _name { get; set; }

        public RTN(string name)
        {
            _name = name; 
            _network = new Dictionary<string, Node>();
        }

        public void AddNode(Node node)
        {
            _network.Add(node._name, node);
        }

        public Node GetNode(string name)
        {
            return _network[name];
        }


    }
}
