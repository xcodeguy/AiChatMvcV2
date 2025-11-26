USE WakeNbake;
DROP PROCEDURE sp_insert_table_response;

CREATE PROCEDURE sp_insert_table_response(
            response LONGTEXT,
            model VARCHAR(100),
            topic TEXT,
            prompt TEXT,
            negative_prompt TEXT,
            active BOOLEAN,
            audio_file_name VARCHAR(250),
            audio_file_size FLOAT,
            response_time DATETIME,
            word_count INT,
            exceptions TEXT,
            tts_voice VARCHAR(100),
            score INT,
            grade INT,
            score_reasons JSON
)

BEGIN
  INSERT INTO Response(
              response,
              model,
              topic,
              prompt,
              negative_prompt,
              active,
              audio_file_name,
              audio_file_size,
              response_time,
              word_count,
              exceptions,
              tts_voice,
              score,
              grade)
  VALUES (
              response,
              model,
              topic,
              prompt,
              negative_prompt,
              active,
              audio_file_name,
              audio_file_size,
              response_time,
              word_count,
              exceptions,
              tts_voice,
              score,
              grade);
END