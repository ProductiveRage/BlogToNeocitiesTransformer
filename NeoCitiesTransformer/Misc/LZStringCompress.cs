using System;
using System.Collections.Generic;

namespace NeoCitiesTransformer.Misc
{
	// Translated from http://pieroxy.net/blog/pages/lz-string/index.html (https://github.com/pieroxy/lz-string/blob/master/libs/lz-string-1.3.0.js)
	public static class LZStringCompress
	{
		public static string CompressToUTF16(string input)
		{
			if (input == null)
				throw new ArgumentNullException("input");

			input = Compress(input);
			if (input.Length == 0)
				return input;

			var output = "";
			var status = 0;
			var current = 0; // Need to give this a default value but it won't matter what it is as the "case 0" will set it properly below
			for (var i = 0; i < input.Length; i++)
			{
				var c = input[i];
				switch (status++)
				{
					case 0:
						output += (char)((c >> 1) + 32);
						current = (c & 1) << 14;
						break;
					case 1:
						output += (char)((current + (c >> 2)) + 32);
						current = (c & 3) << 13;
						break;
					case 2:
						output += (char)((current + (c >> 3)) + 32);
						current = (c & 7) << 12;
						break;
					case 3:
						output += (char)((current + (c >> 4)) + 32);
						current = (c & 15) << 11;
						break;
					case 4:
						output += (char)((current + (c >> 5)) + 32);
						current = (c & 31) << 10;
						break;
					case 5:
						output += (char)((current + (c >> 6)) + 32);
						current = (c & 63) << 9;
						break;
					case 6:
						output += (char)((current + (c >> 7)) + 32);
						current = (c & 127) << 8;
						break;
					case 7:
						output += (char)((current + (c >> 8)) + 32);
						current = (c & 255) << 7;
						break;
					case 8:
						output += (char)((current + (c >> 9)) + 32);
						current = (c & 511) << 6;
						break;
					case 9:
						output += (char)((current + (c >> 10)) + 32);
						current = (c & 1023) << 5;
						break;
					case 10:
						output += (char)((current + (c >> 11)) + 32);
						current = (c & 2047) << 4;
						break;
					case 11:
						output += (char)((current + (c >> 12)) + 32);
						current = (c & 4095) << 3;
						break;
					case 12:
						output += (char)((current + (c >> 13)) + 32);
						current = (c & 8191) << 2;
						break;
					case 13:
						output += (char)((current + (c >> 14)) + 32);
						current = (c & 16383) << 1;
						break;
					case 14:
						output += (char)((current + (c >> 15)) + 32);
						output += (char)((c & 32767) + 32);
						status = 0;
						break;
				}
			}
			return output + (char)(current + 32);
		}

		private static string Compress(string uncompressed)
		{
			var context_dictionary = new Dictionary<string, int>();
			var context_dictionaryToCreate = new Dictionary<string, bool>();
			var context_c = "";
			var context_wc = "";
			var context_w = "";
			var context_enlargeIn = 2; // Compensate for the first entry which should not count
			var context_dictSize = 3;
			var context_numBits = 2;
			var context_data_string = "";
			var context_data_val = 0;
			var context_data_position = 0;

			int value;
			for (var ii = 0; ii < uncompressed.Length; ii += 1)
			{
				context_c = uncompressed[ii].ToString();
				if (!context_dictionary.ContainsKey(context_c))
				{
					context_dictionary[context_c] = context_dictSize++;
					context_dictionaryToCreate[context_c] = true;
				}

				context_wc = context_w + context_c;
				if (context_dictionary.ContainsKey(context_wc))
					context_w = context_wc;
				else
				{
					if (context_dictionaryToCreate.ContainsKey(context_w))
					{
						if (context_w[0] < 256)
						{
							for (var i = 0; i < context_numBits; i++)
							{
								context_data_val = (context_data_val << 1);
								if (context_data_position == 15)
								{
									context_data_position = 0;
									context_data_string += (char)context_data_val;
									context_data_val = 0;
								}
								else
									context_data_position++;
							}
							value = context_w[0];
							for (var i = 0; i < 8; i++)
							{
								context_data_val = (context_data_val << 1) | (value & 1);
								if (context_data_position == 15)
								{
									context_data_position = 0;
									context_data_string += (char)context_data_val;
									context_data_val = 0;
								}
								else
									context_data_position++;
								value = value >> 1;
							}
						}
						else
						{
							value = 1;
							for (var i = 0; i < context_numBits; i++)
							{
								context_data_val = (context_data_val << 1) | value;
								if (context_data_position == 15)
								{
									context_data_position = 0;
									context_data_string += (char)context_data_val;
									context_data_val = 0;
								}
								else
									context_data_position++;
								value = 0;
							}
							value = context_w[0];
							for (var i = 0; i < 16; i++)
							{
								context_data_val = (context_data_val << 1) | (value & 1);
								if (context_data_position == 15)
								{
									context_data_position = 0;
									context_data_string += (char)context_data_val;
									context_data_val = 0;
								}
								else
									context_data_position++;
								value = value >> 1;
							}
						}
						context_enlargeIn--;
						if (context_enlargeIn == 0)
						{
							context_enlargeIn = (int)Math.Pow(2, context_numBits);
							context_numBits++;
						}
						context_dictionaryToCreate.Remove(context_w);
					}
					else
					{
						value = context_dictionary[context_w];
						for (var i = 0; i < context_numBits; i++)
						{
							context_data_val = (context_data_val << 1) | (value & 1);
							if (context_data_position == 15)
							{
								context_data_position = 0;
								context_data_string += (char)context_data_val;
								context_data_val = 0;
							}
							else
								context_data_position++;
							value = value >> 1;
						}
					}
					context_enlargeIn--;
					if (context_enlargeIn == 0)
					{
						context_enlargeIn = (int)Math.Pow(2, context_numBits);
						context_numBits++;
					}
					context_dictionary[context_wc] = context_dictSize++; // Add wc to the dictionary
					context_w = context_c;
				}
			}

			// Output the code for w
			if (context_w != "")
			{
				if (context_dictionaryToCreate.ContainsKey(context_w))
				{
					if (context_w[0] < 256)
					{
						for (var i = 0; i < context_numBits; i++)
						{
							context_data_val = (context_data_val << 1);
							if (context_data_position == 15)
							{
								context_data_position = 0;
								context_data_string += (char)context_data_val;
								context_data_val = 0;
							}
							else
								context_data_position++;
						}
						value = context_w[0];
						for (var i = 0; i < 8; i++)
						{
							context_data_val = (context_data_val << 1) | (value & 1);
							if (context_data_position == 15)
							{
								context_data_position = 0;
								context_data_string += (char)context_data_val;
								context_data_val = 0;
							}
							else
								context_data_position++;
							value = value >> 1;
						}
					}
					else
					{
						value = 1;
						for (var i = 0; i < context_numBits; i++)
						{
							context_data_val = (context_data_val << 1) | value;
							if (context_data_position == 15)
							{
								context_data_position = 0;
								context_data_string += (char)context_data_val;
								context_data_val = 0;
							}
							else
								context_data_position++;
							value = 0;
						}
						value = context_w[0];
						for (var i = 0; i < 16; i++)
						{
							context_data_val = (context_data_val << 1) | (value & 1);
							if (context_data_position == 15)
							{
								context_data_position = 0;
								context_data_string += (char)context_data_val;
								context_data_val = 0;
							}
							else
								context_data_position++;
							value = value >> 1;
						}
					}
					context_enlargeIn--;
					if (context_enlargeIn == 0)
					{
						context_enlargeIn = (int)Math.Pow(2, context_numBits);
						context_numBits++;
					}
					context_dictionaryToCreate.Remove(context_w);
				}
				else
				{
					value = context_dictionary[context_w];
					for (var i = 0; i < context_numBits; i++)
					{
						context_data_val = (context_data_val << 1) | (value & 1);
						if (context_data_position == 15)
						{
							context_data_position = 0;
							context_data_string += (char)context_data_val;
							context_data_val = 0;
						}
						else
							context_data_position++;
						value = value >> 1;
					}
				}
				context_enlargeIn--;
				if (context_enlargeIn == 0)
				{
					context_enlargeIn = (int)Math.Pow(2, context_numBits);
					context_numBits++;
				}
			}

			// Mark the end of the stream
			value = 2;
			for (var i = 0; i < context_numBits; i++)
			{
				context_data_val = (context_data_val << 1) | (value & 1);
				if (context_data_position == 15)
				{
					context_data_position = 0;
					context_data_string += (char)context_data_val;
					context_data_val = 0;
				}
				else
					context_data_position++;
				value = value >> 1;
			}

			// Flush the last char
			while (true)
			{
				context_data_val = (context_data_val << 1);
				if (context_data_position == 15)
				{
					context_data_string += (char)context_data_val;
					break;
				}
				else
					context_data_position++;
			}
			return context_data_string;
		}
	}
}