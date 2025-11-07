- Compare the text in the <text_1> element with the text in the <text_2> 
    element and determine how much "on topic" <text_2> is to <text_1>.
- Your comparison should result in a weighted number between 1 and 8. 
    The more relevant <text_2> is to <text_1> should produce a
    higher number.
- Your response can only be a number between 1 and 8.
- Your response should be JSON formatted.

Template of the expected JSON output:
    {
        "grade": "[response]"
    }

<text_1>
The quick brown fox jumps over the lazy dog. This is a classic pangram, a sentence that contains every letter of the alphabet. It's often used to test fonts or keyboard layouts. The sentence is concise and memorable, making it a popular example in typography and language exercises. It demonstrates a simple action and a contrasting element.
</text_1>

<text_2>
This response discusses the classic pangram 'The quick brown fox jumps over the lazy dog'. It explains that it's used to test fonts or keyboard layouts, and is popular due to its concise and memorable nature.
</text_2>