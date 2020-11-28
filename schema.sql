DROP TABLE IF EXISTS data;
DROP TABLE IF EXISTS transmissions;

CREATE TABLE transmissions (
	id SERIAL,
	transmission_id VARCHAR(36) NOT NULL,
	dt_imported TIMESTAMP NOT NULL,
	PRIMARY KEY(id),
	UNIQUE(transmission_id)
);

CREATE TABLE data (
	id SERIAL,
	sku VARCHAR(32) NOT NULL,
	description TEXT NOT NULL,
	category TEXT NOT NULL,
	price DECIMAL(12,2) NOT NULL,
	location VARCHAR(128) NOT NULL,
	qty INT NOT NULL,
	transmission_id INT NOT NULL,
	PRIMARY KEY(id),
	CONSTRAINT fk_transmission FOREIGN KEY(transmission_id) REFERENCES transmissions(id)
);
