using Ocluse.LiquidSnow.Core.Extensions;
using Ocluse.LiquidSnow.Cryptography.Classical.Internals;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Text;

namespace Ocluse.LiquidSnow.Cryptography.Classical
{
    ///<summary>
    ///Provides functionality for performing cryptographic operations by simulating the Enigma Machine
    ///</summary>
    /// <remarks>
    /// The Enigma machine was used by the German army during World War 2 for top secret communication.
    /// While this class does it's best to simulate the behaviour of the physical device,
    /// there may still be a few places it falls short. Configuration is necessary to obtain desirable behaviour.
    /// </remarks>
    public partial class EnigmaMachine : ClassicalAlgorithm
    {
        #region Constructors
        /// <summary>
        /// Creates a new instance of an Enigma Machine with the provided rotors
        /// </summary>
        public EnigmaMachine(Alphabet alphabet, EnigmaWheel stator, EnigmaWheel reflector, IEnumerable<Rotor> rotors) : base(alphabet)
        {
            Rotors = new ObservableCollection<Rotor>(rotors);
            Alphabet = alphabet;
            Stator = stator;
            Reflector = reflector;
            Rotors.CollectionChanged += OnRotorsChanged;
            Rotors.AddRange(rotors);
        }

        private void OnRotorsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add
                || e.Action == NotifyCollectionChangedAction.Replace)
            {
                foreach (var item in e.NewItems)
                {
                    var rotor = (Rotor)item;
                    rotor.HitNotch += OnRotorHitNotch;
                }
            }
            if (e.Action == NotifyCollectionChangedAction.Remove
                || e.Action == NotifyCollectionChangedAction.Replace
                || e.Action == NotifyCollectionChangedAction.Reset)
            {
                foreach (var item in e.OldItems)
                {
                    var rotor = (Rotor)item;
                    rotor.HitNotch -= OnRotorHitNotch;
                }
            }
        }

        /// <summary>
        /// Creates a builder for a <see cref="EnigmaMachine"/>
        /// </summary>
        /// <returns></returns>
        public static IEnigmaMachineBuilder Create()
        {
            return new EnigmaMachineBuilder();
        }

        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets rotors of the Enigma Machine. 
        /// </summary>
        /// <remarks>
        /// The rotors are the moving parts of the machine. Current traverses through the rotors when a key is pressed and goes through the wiring, 
        /// changing contact points from one rotor to the next. This action scarmbles the letters
        /// </remarks>
        public ObservableCollection<Rotor> Rotors { get; private set; }

        ///// <summary>
        ///// Gets or sets a value indicating whether the rotors should be reset to the key position after each run.
        ///// </summary>
        //public bool AutoReset { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating if double stepping should be simulated.
        /// </summary>
        public bool DoubleStep { get; set; }

        /// <summary>
        /// Gets or sets the ETW of the machine.
        /// </summary>
        /// <remarks>
        /// This is the first 'wheel' that the electric current flows into before heading to the actual rotors.
        /// It is fixed and does not rotate.
        /// </remarks>
        public EnigmaWheel Stator { get; set; }

        /// <summary>
        /// Gets or sets the reflector of the machine
        /// </summary>
        /// <remarks>
        /// Reflects the electric current back through the wheels. This action is what enables the Enigma encryption to be reversable.
        /// </remarks>
        public EnigmaWheel Reflector { get; set; }

        /// <summary>
        /// Gets or sets the machine's plugboard.
        /// </summary>
        /// <remarks>
        /// The plugboard switch allows for further scrambling by substituting character pairs.
        /// </remarks>
        public Plugboard? Plugboard { get; set; }

        /// <summary>
        /// Gets the current rotation of each of the rotors.
        /// </summary>
        /// <remarks>
        /// Returns an array of integers, with each representing the index of rotation of the currently visible character through the window.
        /// </remarks>
        public int[] RotorConfig
        {
            get
            {
                if (Alphabet == null)
                {
                    throw new InvalidOperationException("Alphabet is null");
                }

                var list = new List<int>();

                foreach (var window in Windows)
                {
                    list.Add(Alphabet.IndexOf(window));
                }

                return list.ToArray();
            }
        }

        /// <summary>
        /// Returns a <see cref="char"/> array of the characters currently visible through the window.
        /// </summary>
        public char[] Windows
        {
            get
            {
                if (Rotors == null)
                    throw new NullReferenceException("Machine has no rotors");
                var list = new List<char>();

                foreach (var rotor in Rotors)
                {
                    list.Add(rotor.Window);
                }

                return list.ToArray();
            }
        }

        #endregion

        #region Predefined Machines

        /// <summary>
        /// A133 is a special variant of EnigmaB, that was delivered to the Swedish SGS on 6 April 1925.
        /// It uses 28 letters, with letters Å, Ä and Ö common in the Swedish language. It lacks letter W though.
        /// </summary>
        public static EnigmaMachine A133
        {
            get =>
                new EnigmaMachine(new Alphabet("ABCDEFGHIJKLMNOPQRSTUVXYZÅÄÖ"), EnigmaWheel.A133_ETW, EnigmaWheel.A133_UKW, new Rotor[]
                    {
                        EnigmaWheel.A133_I,
                        EnigmaWheel.A133_II,
                        EnigmaWheel.A133_III
                    })
                {
                    DoubleStep = true
                };
        }

        /// <summary>
        /// Enigma D, or Commercial Enigma A26,  was introduced in 1926 and served as the basis for most
        /// of the later machines.
        /// </summary>
        public static EnigmaMachine A26
        {
            get =>
                new EnigmaMachine(new Alphabet("ABCDEFGHIJKLMNOPQRSTUVWXYZ"), EnigmaWheel.A26_ETW, EnigmaWheel.A26_UKW, new Rotor[]
                {
                        EnigmaWheel.A26_I,
                        EnigmaWheel.A26_II,
                        EnigmaWheel.A26_III
                })
                {
                    DoubleStep = true
                };
        }

        /// <summary>
        /// The Enigma I was the main machine used by the German Army and Air Force, <i>Luftwaffe</i>. UKW_B was the 
        /// standard reflector.
        /// </summary>
        public static EnigmaMachine Enigma_I
        {
            get =>
                new EnigmaMachine(new Alphabet("ABCDEFGHIJKLMNOPQRSTUVWXYZ"), EnigmaWheel.E1_ETW, EnigmaWheel.E1_UKW_B, new Rotor[]
                {
                    EnigmaWheel.E1_I,
                    EnigmaWheel.E1_II,
                    EnigmaWheel.E1_III
                })
                {
                    DoubleStep = true
                };
        }

        /// <summary>
        /// Immediately after the War in 1945, some captured Enigma-I machines were used by the former 
        /// Norwegian Police Security Service: <i>Overvaakingspolitiet.</i> They modified the wiring
        /// and the UKW. The ETW and position of turnover notches were left unaltered.
        /// </summary>
        public static EnigmaMachine Norenigma
        {
            get =>
                new EnigmaMachine(new Alphabet("ABCDEFGHIJKLMNOPQRSTUVWXYZ"), EnigmaWheel.NE_ETW, EnigmaWheel.NE_UKW, new Rotor[]
                {
                        EnigmaWheel.NE_I,
                        EnigmaWheel.NE_II,
                        EnigmaWheel.NE_III
                })
                {
                    DoubleStep = true
                };
        }

        /// <summary>
        /// In the late 1980s, a strange Enigma machine was discovered in the house of a former intelligence officer,
        /// who used to work for a special unit. It was a strandard Enigma I with the UKW changed.
        /// The wheels were marked with the letter <b>S</b>, which probably means <i>Sondermaschine</i>(special machine)
        /// </summary>
        public static EnigmaMachine Sondermaschine
        {
            get =>
                new EnigmaMachine(new Alphabet("ABCDEFGHIJKLMNOPQRSTUVWXYZ"), EnigmaWheel.SE_ETW, EnigmaWheel.SE_UKW, new Rotor[]{
                        EnigmaWheel.SE_I,
                        EnigmaWheel.SE_II,
                        EnigmaWheel.SE_III
                    })
                {
                    DoubleStep = true
                };
        }

        /// <summary>
        /// The M1, M2 and M3 Enigma machines were used by the German Navy(<i>Kriegsmarine</i>). They
        /// are basically compatible with Enigma I.
        /// </summary>
        public static EnigmaMachine Enigma_M3
        {
            get =>
                new EnigmaMachine(new Alphabet("ABCDEFGHIJKLMNOPQRSTUVWXYZ"), EnigmaWheel.M3_ETW, EnigmaWheel.M3_UKW_B, new Rotor[]
                {
                        EnigmaWheel.M3_I,
                        EnigmaWheel.M3_II,
                        EnigmaWheel.M3_III
                })
                {
                    DoubleStep = true
                };
        }

        /// <summary>
        /// This was a further development of the M3 and was used exclusively by the U-boat division of the 
        /// Kriegsmarine. It actually featured four rotors, unlike all the other Enigma Machines. Its extra wheels had 
        /// two notches, instead of the standard one.
        /// </summary>
        public static EnigmaMachine Enigma_M4
        {
            get =>
                new EnigmaMachine(new Alphabet("ABCDEFGHIJKLMNOPQRSTUVWXYZ"), EnigmaWheel.M4_ETW, EnigmaWheel.M4_UKW_B, new Rotor[]
                {
                        EnigmaWheel.M4_I,
                        EnigmaWheel.M4_II,
                        EnigmaWheel.M4_III,
                        EnigmaWheel.M4_IV
                })
                {
                    DoubleStep = true
                };
        }

        /// <summary>
        /// Creates an enigma machine with a random configuration, whose alphabet is the ASCII character set.
        /// </summary>
        public static EnigmaMachine RandomASCII
        {
            get => Create().Build();
        }
        #endregion

        #region Private Methods

        private void OnRotorHitNotch(Rotor rotor, bool forward)
        {
            var index = Rotors.IndexOf(rotor);

            if (index == -1) throw new ArgumentException("The rotor that hit the notch was not found", nameof(rotor));

            if (index == Rotors.Count - 1) return;//the last rotor, no need to rotate

            //Perform any necessary double stepping
            if (DoubleStep && index + 1 >= Rotors.Count)
            {
                if (Rotors[index + 1].IsTurnOver())
                {
                    Rotors[index + 1].Rotate();
                }
            }

            index++; //Rotate the next rotor;
            Rotors[index].Rotate(forward);
        }
        #endregion

        #region Public Methods

        /// <summary>
        /// Relaligns the rotors to a provided key
        /// </summary>
        public void ResetRotors(string key)
        {
            if (key.Length < Rotors.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(key), "The length of the key must match the number of rotors");
            }
            int i = 0;
            foreach (var rotor in Rotors)
            {
                var window = key[i];
                rotor.Reset(window);
                i++;
            }
        }

        #endregion

        #region Logic Methods

        /// <inheritdoc/>
        /// <remarks>
        /// This methods call <see cref="ResetRotors(string)"/> before and after running the algorithm using the provided <paramref name="key"/>.
        /// To run the algorithm without resetting the positions of the rotors, call <see cref="Run(string)"/>.
        /// Also not that the value of <paramref name="forward"/> is ignored.
        /// </remarks>
        public override string Run(string input, string key, bool forward)
        {
            ResetRotors(key);
            
            string output = Run(input);

            ResetRotors(key);

            return output.ToString();
        }

        /// <summary>
        /// Runs the algorithm, without changing the rotor positions.
        /// </summary>
        /// <returns>The output of the run.</returns>
        public string Run(string input)
        {
            var output = new StringBuilder();

            foreach (var c in input)
                output.Append(Run(c));

            return output.ToString();
        }

        /// <summary>
        /// A special run for the Enigma. Simulates a key press and returns a result.
        /// </summary>
        /// <param name="input">The character that has been depressed</param>
        /// <returns>The output, i.e the character that will be lit in the lamp</returns>
        public virtual char Run(char input)
        {
            //Rotate the FastRotor
            Rotors[0].Rotate();

            //Get the index of the letter in the alphabet:
            int index = Alphabet.IndexOf(input);

            //Pass the current to the Stator:
            index = Stator.GetPath(index, true);

            //Pass the current through successive rotors:
            for (int i = 0; i < Rotors.Count; i++)
            {
                index = Rotors[i].GetPath(index, true);
            }

            //Pass the current the Reflector
            index = Reflector.GetPath(index, true);

            //Pass through the rotors in reverse:
            for (int i = Rotors.Count - 1; i > -1; i--)
            {
                var rotor = Rotors[i];
                index = rotor.GetPath(index, false);
            }

            //Pass the current through the Stator:
            index = Stator.GetPath(index, false);

            var result = Alphabet[index];

            //perform plugboard simulation:
            if (Plugboard != null)
            {
                result = Plugboard.Simulate(result);
            }
            return result;

        }
        #endregion

        #region IO Methods
        /// <summary>
        /// Saves the current enigma machine to the specified stream using a specific codec.
        /// </summary>
        /// <remarks>
        /// This method saves the alphabet and the confiugration of the rotors, including the ETw and UKW
        /// </remarks>
        /// <param name="stream">The stream to save the machine options to</param>
        public void Save(Stream stream)
        {
            //header
            using var writer = new BinaryWriter(stream);

            //Header
            writer.Write("EGMC");

            //Alphabet
            writer.Write("alph");
            writer.Write(Alphabet.Count);
            writer.Write(Alphabet.ToString());

            //Stator Indexing:
            writer.Write("ETW ");
            writer.Write("indx");
            writer.Write(Stator.Indexing.Count);
            writer.Write(Stator.Indexing.ToString());

            //Stator Wiring
            writer.Write("wire");
            writer.Write(Stator.Wiring.Count);
            writer.Write(Stator.Wiring.ToString());

            //Reflector Indexing
            writer.Write("UKW ");
            writer.Write("indx");
            writer.Write(Reflector.Indexing.Count);
            writer.Write(Reflector.Indexing.ToString());

            //Reflector Wiring
            writer.Write("wire");
            writer.Write(Reflector.Wiring.Count);
            writer.Write(Reflector.Wiring.ToString());

            //Rotors
            writer.Write("ROTS");
            writer.Write(Rotors.Count);

            foreach (var rotor in Rotors)
            {
                //Indexing
                writer.Write("indx");
                writer.Write(rotor.Indexing.Count);
                writer.Write(rotor.Indexing.ToString());

                //Wiring
                writer.Write("wire");
                writer.Write(rotor.Wiring.Count);
                writer.Write(rotor.Wiring.ToString());

                //Turnover Notch
                writer.Write("ntch");
                writer.Write(rotor.TurnOver.Count);
                foreach (var notch in rotor.TurnOver)
                {
                    writer.Write(notch);
                }
            }
        }

        /// <summary>
        /// Loads an enigma machine configruration from the provided stream.
        /// </summary>
        /// <param name="stream">The stream containing the Enigma Machine configuration</param>
        /// <returns>An <see cref="EnigmaMachine"/> represented by the configuration in the <paramref name="stream"/></returns>
        /// <exception cref="FormatException">When the stream is not a valid enigma machine configuration.</exception>
        public static EnigmaMachine Load(Stream stream)
        {

            using var reader = new BinaryReader(stream);

            //HEADER
            var buffer = reader.ReadString();
            if (buffer != "EGMC") throw new FormatException("Not a valid Enigma Machine File");


            //ALPHABET
            buffer = reader.ReadString();
            if (buffer != "alph") throw new FormatException("Not a valid Enigma Machine File");
            var len = reader.ReadInt32();
            buffer = reader.ReadString();
            var alphabet = new Alphabet(buffer);

            //STATOR AND REFLECTOR
            var list = new string[] { "ETW ", "UKW " };
            var wheels = new List<EnigmaWheel>(2);
            foreach (var item in list)
            {
                buffer = reader.ReadString();
                if (buffer != item) throw new FormatException("Not a valid Enigma Machine File");
                buffer = reader.ReadString();
                if (buffer != "indx") throw new FormatException("Not a valid Enigma Machine File");
                len = reader.ReadInt32();
                buffer = reader.ReadString();
                var indexing = new Alphabet(buffer);

                buffer = reader.ReadString();
                if (buffer != "wire") throw new FormatException("Invalid/Corrupted Enigma File");
                len = reader.ReadInt32();
                buffer = reader.ReadString();
                var wiring = new Alphabet(buffer);

                var wheel = new EnigmaWheel(indexing, wiring);
                wheels.Add(wheel);
            }

            //ROTORS
            buffer = reader.ReadString();
            if (buffer != "ROTS") throw new FormatException("Invalid/Corrupted Enigma File");

            len = reader.ReadInt32();

            var builder = new StringBuilder();
            var rotors = new List<Rotor>();
            while (true)
            {
                builder.Clear();
                buffer = reader.ReadString();
                if (buffer != "indx")
                    throw new FormatException("Invalid/Corrupted Enigma File");
                len = reader.ReadInt32();
                buffer = reader.ReadString();
                var indexing = new Alphabet(buffer);

                buffer = reader.ReadString();
                if (buffer != "wire")
                    throw new FormatException("Invalid/Corrupted Enigma File");
                len = reader.ReadInt32();
                buffer = reader.ReadString();
                var wiring = new Alphabet(buffer);
                var wheel = new EnigmaWheel(indexing, wiring);

                buffer = reader.ReadString();
                if (buffer != "ntch")
                    throw new FormatException("Invalid/Corrupted Enigma File");

                len = reader.ReadInt32();
                for (int t = 0; t < len; t++)
                {
                    builder.Append(reader.ReadChar());
                }

                var rotor = new Rotor(indexing, wiring, builder.ToString());
                rotors.Add(rotor);

                if (reader.PeekChar() == -1) break;
            }

            //BUILDER
            return Create()
                .WithAlphabet(alphabet)
                .WithRotors(rotors)
                .WithStator(wheels[0])
                .WithReflector(wheels[1])
                .Build();
        }
        #endregion
    }
}