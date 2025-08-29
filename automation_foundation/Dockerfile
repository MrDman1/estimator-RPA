
FROM python:3.11-slim

# System deps for Playwright and WeasyPrint
RUN apt-get update && apt-get install -y     curl wget gnupg build-essential libffi-dev libpango-1.0-0 libpangoft2-1.0-0     libpangocairo-1.0-0 libcairo2 libjpeg62-turbo libpng16-16 libgdk-pixbuf-2.0-0     fonts-liberation libnss3 libxkbcommon0 libxcomposite1 libxdamage1 libxfixes3     libxrandr2 libasound2 libatk1.0-0 libgtk-3-0 xvfb     && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

# Install Playwright browsers
RUN python -m playwright install --with-deps

COPY . .
