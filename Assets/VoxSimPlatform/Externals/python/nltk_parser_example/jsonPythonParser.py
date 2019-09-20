# Okay, this one will return a JSON file corresponding to a rudimentary nltk parse of a sentence with limited vocabulary

import nltk
import json

class INLParser:
    def __init__(self):
        pass

    def NLParse(self, rawSent):
        return rawSent

    def InitParserService(self, address):
        pass

class JsonParser(INLParser):
    def __init__(self):
        # Lists, formatted into the strings nltk's CFG expects
        nouns = ["block", "ball", "plate", "cup", "disc", "spoon", "fork", "book", "blackboard", "bottle", "grape", "apple", "banana", "table", "bowl", "knife", "pencil", "paper_sheet", "mug", "lid", "stack", "staircase", "pyramid", "cork"]
        noun_list = "'" + "' | '".join(nouns) + "'"
        # Currently requires hard-coded "in front of" -> "in_front_of" etc.
        preps = ["touching",  "in",  "on",  "at",  "behind",  "in_front_of",  "near",  "left_of",  "right_of",  "center_of",  "edge_of",  "under",  "against"]
        prep_list = "'" + "' | '".join(preps)+ "'"
        self.grammar = nltk.CFG.fromstring("""
                S -> VP
                PP -> P NP
                NP -> Det N | Det Adj N | Det N PP | 'I'
                VP -> V NP | VP PP
                Det -> 'the' | 'a' | 'this' | 'that' | 'two' | 'an' | 'my'
                N ->""" + noun_list + """
                V -> 'shot' | 'put' | 'lift' | 'grasp' | 'grab'
                P ->""" + prep_list + """
                Adj -> 'red' | 'orange' | 'yellow' | 'green' | 'blue' | 'purple' | 'white' | 'black' | 'brown' | 'gray' | 'middle' | 'leftmost' | 'rightmost'
                """)

    def NLParse(self, rawSent):
        test_sent = rawSent

        # Awkward way to turn multiword phrases into terminals for nltk. All preposition terminals atm
        # More robust grammars could better handle these
        single_wordify = {
            "in front of" : "in_front_of",
            "left of" : "left_of",
            "right of" : "right_of",
            "center of" : "center_of",
            "edge of" : "edge_of"
        }
        for phrase in single_wordify.keys():
            test_sent = test_sent.replace(phrase, single_wordify[phrase])
        tokenizer = nltk.tokenize._treebank_word_tokenizer
        toks = tokenizer.tokenize(test_sent.lower())
        parser = nltk.ChartParser(self.grammar)
        return(dict_to_json(tree_to_dict(parser.parse(toks))))



# https://stackoverflow.com/questions/23112284/convert-nltk-tree-to-json-representation
def tree_to_dict(tree):
    tdict = {}
    for t in tree:
        if isinstance(t, nltk.Tree) and isinstance(t[0], nltk.Tree):
            tdict[t.label()] = tree_to_dict(t)
        elif isinstance(t, nltk.Tree):
            tdict[t.label()] = t[0]
    return tdict

def dict_to_json(dict):
    return json.dumps(dict)



if __name__ == "__main__":
    # Zero effort grammar for test purposes. Which of course means it's going into production.
    jsp = JsonParser()
    test_sent = "Put the yellow block right of the purple block"
    print(jsp.NLParse(test_sent))

