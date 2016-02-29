# Locator Service #

Many micro-blogs contain information that is relevant for maintaining safety and security, for example
while managing an event. The problem is that many micro-blogs (tweets, facebook messages, etc.) do not contain
any exact location information like the sender's GPS position. However, the message may contain a reference to 
a location that is understandable by humans, like "let's meet at the central (train) station in Delft." The 
goal of this project is therefore to create a service that can help determine the location of a micro-blog's 
content in The Netherlands. 

## Approach ##

The approach first tackles the problem of generating a describtion of a neighbourhood, e.g. by naming streets 
and public locations. Next, a micro-blog is taken and checked whether it generates a good match. 

An optional second step is to try to find a more precise location, e.g. the location of the train station.

## Filling the indexing service ##

1. Use the CBS neighbourhood (buurt) data to obtain the neighbourhood boundary.
2. Use the Kadaster BAG data to obtain the street names inside a boundary.
3. Use the OSM Netherlands data to obtain the name of (bus/train) stations, cafes, pubs and other 
   amenities inside a boundary.
4. Feed the aggregated neighbourhood data to an Apache SOLR service for indexing.

### Implementation details ###

The implementation can be found in GenerateLocationData. Here, a pipeline structure is implemented, where each
filter expands the current set of data for a neighbourhood. This data is stored in a Dictionary, where the key is 
the name of the data set, and the value a list of descriptive names.


## Querying the indexing service ##

