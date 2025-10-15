# Use curabase for non-repetative builds
FROM curabase

# Copy the package.json and package-lock.json files into the container
COPY package*.json ./

# Install dependencies
RUN npm install

COPY server.ts ./
COPY slicer_options.json ./
COPY tsconfig.json ./

# Expose the port that the server will listen on
EXPOSE 3000

# Start the server
CMD ["npm", "start"]
