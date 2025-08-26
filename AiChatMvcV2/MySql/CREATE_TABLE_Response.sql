/*
generate a MySql create table staement for a table name ChatHistory with a column named timestamp which will be used for a
...  timestamp, a column named chat_text which will sontain an infinite number of VARCHAR data, a column named model which will c
... ontain VARCHAR data of varying lengths, a column named topic which will contain VARCHAR data of varying lengths, a column named 
... prompt which will contain an infinie number of VARCHAR data, a column named negative_prompt which will contain an infinite nu
... mber of VARCHAR data, a column named active which will contain a boolean value, a column named last_updated which will contai
... n a data and time stamp, a column named exceptions which will contain a numeric value with no decimals, a column named res
... ponse_seconds which will contain a numeric value with no decimals, a column named word_count which will contain numeric da
... ta with no decimal.

*/
USE WakeNbake;

CREATE TABLE Response (
            timestamp TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
            response LONGTEXT NOT NULL,
            model VARCHAR(100),
            topic VARCHAR(100),
            prompt TEXT,
            negative_prompt TEXT,
            active BOOLEAN,
            last_updated TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
            exceptions INT UNSIGNED NOT NULL,
            response_time DATETIME,
            word_count INT UNSIGNED NOT NULL
            );



    

