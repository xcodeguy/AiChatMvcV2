USE WakeNbake;
DROP PROCEDURE sp_insert_table_response;

CREATE PROCEDURE sp_insert_table_response(
    timestamp DATETIME,
    response LONGTEXT,
    model VARCHAR(100),
    topic VARCHAR(100),
    prompt TEXT,
    negative_prompt TEXT,
    active BIT,
    last_updated DATETIME,
    response_time INT,
    word_count INT
)

BEGIN
  INSERT INTO Response(
    timestamp,
    response,
    model,
    topic,
    prompt,
    negative_prompt,
    active,
    last_updated,
    response_time,
    word_count)
  VALUES (
    timestamp,
    response,
    model,
    topic,
    prompt,
    negative_prompt,
    active,
    last_updated,
    response_time,
    word_count);
END