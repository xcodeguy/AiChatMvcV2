 - Perform Task A and apply the following Instructions. 

Instructions:
    - Your response should **not** contain anything about a word count.
    - Remove any sentence that contains the string **“Task A”** or **"Task B"**.
    - Remove any sentence that contains the word **“topic”**.
    - Remove any sentence that contains the string **“I understand”**.
    - Remove any **-** characters.
    - Remove any duplicate sentences and anything associated with those sentences such as numbers.
        For example: "1. Some example text.".
    - Remove any carriage return, line feed characters, and format strings.
    - All text comparisons are **case insensitive**.
    - After the response, also summarize the response using one or two words. 
        Put the summary in the "topic" key value of the JSON output.
    - Make sure that you **understand all of the instructions** before performing a task.
    - The JSON output cannot use arrays.
    - The JSON output must have all keys and values enclosed in double quotation marks.
    - Your final output should be valid JSON only.
    
Task A:
    - If the <LastResponse> element is empty then pick a topic about anything you know.
    - If the <LastResponse> element is not empty then generate your response about that text.
    Notes:
        - The topic can be about anything.
        - The topic must **not** be about Bioluminescence.
        - The topic must **not** be about coffee.
    <LastResponse>
    </LastResponse>
<topic>
</topic>

Output JSON Format:
{
    ""response"" : response,
    ""topic"" : topic
}