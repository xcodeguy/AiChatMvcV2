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
    - After the response, also summarize the topic using one or two words and put them in the <topic> element.
    - Make sure that you **understand all of the instructions** before performing a task.
    
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
    "response" : [response],
    "topic" : [<topic></topic>]
}